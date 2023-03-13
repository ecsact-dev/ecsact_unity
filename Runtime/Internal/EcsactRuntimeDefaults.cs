using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using Ecsact.UnitySync;
using System;

[assembly:InternalsVisibleTo("EcsactRuntime")]

#nullable enable

namespace Ecsact.Internal {

internal static class EcsactRuntimeDefaults {
	private static bool                        unitySyncScriptsRegistered = false;
	private static List<global::System.Action> cleanupFns = new();

	internal static CacheRegistry? cacheRegistry;

	[RuntimeInitializeOnLoadMethod]
	private static void RuntimeInit() {
		UnityEngine.Application.quitting += OnQuit;
	}

	private static void OnQuit() {
		Cleanup();
	}

	private static void Cleanup() {
		try {
			foreach(var fn in cleanupFns) {
				fn();
			}
		} finally {
			cleanupFns.Clear();
		}
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void Setup() {
		var settings = EcsactRuntimeSettings.Get();

		Ecsact.Defaults._Runtime = EcsactRuntime.Load(settings.runtimeLibraryPaths);

		if(Ecsact.Defaults._Runtime == null) {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			var okQuit = UnityEditor.EditorUtility.DisplayDialog(
				title: "Failed to load default ecsact runtime",
				message: "Please check your ecsact runtime settings",
				ok: "Ok Quit",
				cancel: "Continue Anyways"
			);

			if(okQuit) {
				UnityEditor.EditorApplication.isPlaying = false;
			}
			UnityEngine.Application.Quit(1);
#else
			UnityEngine.Debug.LogError("Failed to load default ecsact runtime");
			UnityEngine.Application.Quit(1);
#endif
			throw new Exception("Failed to load default ecsact runtime");
		}

		var registry_id = Ecsact.Defaults.Runtime.core.CreateRegistry(
			settings.defaultRegistry.registryName
		);

		var reg = new Ecsact.Registry(Ecsact.Defaults.Runtime, registry_id);

		// If async no default available registry!
		if(settings.runner == EcsactRuntimeSettings.RunnerType.AsyncRunner) {
			cacheRegistry = new CacheRegistry(Ecsact.Defaults.Runtime, reg);
		} else if(settings.runner == EcsactRuntimeSettings.RunnerType.DefaultRunner) {
			settings.defaultRegistry!.registryId = registry_id;

			Ecsact.Defaults._Registry = reg;
		}

		SetDefaultsRunner(settings);

		EntityGameObjectPool ? pool;

		if(settings.enableUnitySync) {
			SetupUnitySync(Ecsact.Defaults.Runtime, reg, settings, out pool);
			if(!unitySyncScriptsRegistered) {
				RegisterUnitySyncScripts(settings);
			}

			Ecsact.Defaults.Pool = pool;
		}

		Ecsact.Defaults.NotifyReady();
	}

	internal static void ClearDefaults() {
		Cleanup();
		Ecsact.Defaults.ClearDefaults();
	}

	private static void SetDefaultsRunner(EcsactRuntimeSettings settings) {
		var defReg = settings.defaultRegistry;

		if(settings.runner == EcsactRuntimeSettings.RunnerType.DefaultRunner) {
			if(defReg.updateMethod == EcsactRuntimeDefaultRegistry.UpdateMethod.None) {
				Ecsact.Defaults.Runner = null;
			} else if(defReg.updateMethod == EcsactRuntimeDefaultRegistry.UpdateMethod.FixedUpdate) {
				Ecsact.Defaults.Runner =
					EcsactRunner.CreateInstance<DefaultFixedRunner>(
						settings,
						"Default Fixed Runner"
					);
			} else if(defReg.updateMethod == EcsactRuntimeDefaultRegistry.UpdateMethod.Update) {
				Ecsact.Defaults.Runner = EcsactRunner.CreateInstance<DefaultRunner>(
					settings,
					"Default Runner"
				);
			}
		}

		if(settings.runner == EcsactRuntimeSettings.RunnerType.AsyncRunner) {
			Ecsact.Defaults.Runner =
				EcsactRunner.CreateInstance<AsyncRunner>(settings, "Async Runner");
		}
	}

	private static void SetupUnitySync(
		EcsactRuntime         runtime,
		Ecsact.Registry       registry,
		EcsactRuntimeSettings settings,
		out Ecsact.UnitySync.EntityGameObjectPool pool
	) {
		// Debug.Assert(
		// 	registry.pool == null,
		// 	"EntityGameObjectPool already created. SetupUnitySync should only be " +
		// 		"called once."
		// );

		var initPool = EntityGameObjectPool.CreateInstance(
			new RegistryEntitySource(registry.ID, runtime)
		);

		cleanupFns.AddRange(new List<global::System.Action> {
			runtime.OnInitComponent((entity, compId, compData) => {
				initPool.InitComponent(entity, compId, in compData);
			}),
			runtime.OnUpdateComponent((entity, compId, compData) => {
				initPool.UpdateComponent(entity, compId, in compData);
			}),
			runtime.OnRemoveComponent((entity, compId, compData) => {
				initPool.RemoveComponent(entity, compId, in compData);
			}),
		});

		pool = initPool;
	}

	private static void RegisterUnitySyncScripts(EcsactRuntimeSettings settings) {
		foreach(var scriptInfo in settings.unitySyncScripts!) {
			if(!scriptInfo.scriptEnabled) continue;

			var monoStr = scriptInfo.scriptAssemblyQualifiedName;
			var type = global::System.Type.GetType(monoStr);
			if(type == null) {
				Debug.LogError($"Unity Sync: MonoBehaviour {monoStr} not found.");
			} else {
				if(UnitySyncMonoBehaviours.RegisterMonoBehaviourType(type)) {
					Debug.Log($"Registered unity sync mono behaviour: {type.FullName}");
				} else {
					Debug.LogError(
						$"Failed to register unity sync mono behaviour: {type.FullName}"
					);
				}
			}
		}

		unitySyncScriptsRegistered = true;
	}
}

} // namespace Ecsact.Internal
