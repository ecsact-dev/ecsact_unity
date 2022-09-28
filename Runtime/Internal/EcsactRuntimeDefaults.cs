using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Ecsact.UnitySync;

[assembly: InternalsVisibleTo("EcsactRuntime")]

#nullable enable

namespace Ecsact.Internal {

internal static class EcsactRuntimeDefaults {
	private static bool unitySyncScriptsRegistered = false;
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

	internal static void Setup
		( EcsactRuntime          runtime
		, EcsactRuntimeSettings  settings
		)
	{
		foreach(var defReg in settings.defaultRegistries) {
			defReg.registryId = runtime.core.CreateRegistry(
				defReg.registryName
			);

			if(settings.useUnitySync) {
				SetupUnitySync(runtime, defReg);
			}
		}

		if(settings.useUnitySync) {
			if(!unitySyncScriptsRegistered) {
				RegisterUnitySyncScripts(settings);
			}
		}
	}

	private static void SetupUnitySync
		( EcsactRuntime                 runtime
		, EcsactRuntimeDefaultRegistry  defReg
		)
	{
		Debug.Assert(
			defReg.pool == null,
			"EntityGameObjectPool already created. SetupUnitySync should only be " +
			"called once."
		);
		defReg.pool = EntityGameObjectPool.CreateInstance(
			new RegistryEntitySource(defReg.registryId, runtime)
		);

		cleanupFns.AddRange(new List<global::System.Action>{
			runtime.OnInitComponent((entity, compId, compData) => {
				defReg.pool.InitComponent(entity, compId, in compData);
			}),
			runtime.OnUpdateComponent((entity, compId, compData) => {
				defReg.pool.UpdateComponent(entity, compId, in compData);
			}),
			runtime.OnRemoveComponent((entity, compId, compData) => {
				defReg.pool.RemoveComponent(entity, compId, in compData);
			}),
		});
	}

	private static void RegisterUnitySyncScripts
		( EcsactRuntimeSettings  settings
		)
	{
		foreach(var monoStr in settings.unitySyncScripts) {
			var type = global::System.Type.GetType(monoStr + ",Assembly-CSharp");
			if(type == null) {
				throw new global::System.Exception(
					$"Unity Sync: MonoBehaviour {monoStr} not found."
				);
			} else {
				if(UnitySyncMonoBehaviours.RegisterMonoBehaviourType(type)) {
					Debug.Log(
						$"Registered unity sync mono behaviour: {type.FullName}"
					);
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
