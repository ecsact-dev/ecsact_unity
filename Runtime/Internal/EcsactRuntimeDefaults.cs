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

	[RuntimeInitializeOnLoadMethod]
	private static void RuntimeInit() {
		UnityEngine.Application.quitting += OnQuit;
	}

	private static void OnQuit() {
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

		// If async, no default available registry!
		if(settings.runner != EcsactRuntimeSettings.RunnerType.AsyncRunner) {
			settings.defaultRegistry!.registryId = registry_id;
		}

		SetRunner(settings);

		var reg = new Ecsact.Registry(Ecsact.Defaults.Runtime, registryId);

		if(settings.enableUnitySync) {
			SetupUnitySync(
				Ecsact.Defaults.Runtime,
				settings.defaultRegistry,
				settings
			);
			if(!unitySyncScriptsRegistered) {
				RegisterUnitySyncScripts(settings);
			}
			EntityGameObjectPool ? pool;
			pool = settings.defaultRegistry.pool;
			Ecsact.Defaults.Pool = pool;
		}

		Ecsact.Defaults._Registry = reg;
		Ecsact.Defaults.NotifyReady();
	}

	internal static void ClearDefaults() {
		Ecsact.Defaults.ClearDefaults();
	}

	private static void SetRunner(
		EcsactRuntimeSettings settings,
		Int32                 registry_id
	) {
		var defReg = settings.defaultRegistry;

		if(settings.runner == EcsactRuntimeSettings.RunnerType.DefaultRunner) {
			if(defReg.updateMethod == EcsactRuntimeDefaultRegistry.UpdateMethod.None) {
				Ecsact.Defaults.Runner = null;
			} else {
				Ecsact.Defaults.Runner =
					EcsactRunner.CreateInstance<DefaultFixedRunner>(
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
		EcsactRuntime                runtime,
		EcsactRuntimeDefaultRegistry registry,
		EcsactRuntimeSettings        settings
	) {
		Debug.Assert(
			registry.pool == null,
			"EntityGameObjectPool already created. SetupUnitySync should only be " +
				"called once."
		);

		var id = Ecsact.Defaults.Runtime.core.CreateRegistry("async_reg");

		registry.pool = EntityGameObjectPool.CreateInstance(
			new RegistryEntitySource(registry.registryId, runtime)
		);

		// NOTE: This only updates pool
		// I need to set registry updates
		// Need to set up a caching registry to be updated on

		// Problem
		// Flushing updates (ex: gravity) triggers the handler
		// Adding to the cache registry will not work
		// The "update" change when applied to the registry will trigger another
		// "update" change Infinite loop

		cleanupFns.AddRange(new List<global::System.Action> {
			runtime.OnInitComponent((entity, compId, compData) => {
				registry.pool.InitComponent(entity, compId, in compData);
			}),
			runtime.OnUpdateComponent((entity, compId, compData) => {
				registry.pool.UpdateComponent(entity, compId, in compData);
			}),
			runtime.OnRemoveComponent((entity, compId, compData) => {
				registry.pool.RemoveComponent(entity, compId, in compData);
			}),
		});
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
