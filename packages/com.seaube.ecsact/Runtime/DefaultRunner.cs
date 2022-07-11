using UnityEngine;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class DefaultRunner : MonoBehaviour {
		private EcsactRuntime? runtimeInstance = null;

		private EcsactRuntimeDefaultRegistry? defReg;

#if UNITY_EDITOR
		[NonSerialized]
		public int debugExecutionCountTotal = 0;
		[NonSerialized]
		public int debugExecutionTimeMs = 0;
#endif

		[RuntimeInitializeOnLoadMethod]
		private static void OnRuntimeLoad() {
			var settings = EcsactRuntimeSettings.Get();

			foreach(var defReg in settings.defaultRegistries) {
				if(!defReg.useDefaultRunner) continue;

				var gameObjectName = $"Ecsact Default Runner ";
				if(!string.IsNullOrWhiteSpace(defReg.registryName)) {
					gameObjectName += " - " + defReg.registryName;
				}
				gameObjectName += $"({defReg.registryId})";

				var gameObject = new GameObject(gameObjectName);
				var runner = gameObject.AddComponent<DefaultRunner>();
				runner.defReg = defReg;
				DontDestroyOnLoad(gameObject);
			}
		}

		void Awake() {
			runtimeInstance = EcsactRuntime.GetOrLoadDefault();
		}

		void Update() {
			if(defReg == null) return;
			Debug.Assert(defReg.registryId != -1);

#if UNITY_EDITOR
			var executionTimeWatch = System.Diagnostics.Stopwatch.StartNew();
#endif

			runtimeInstance!.core.ExecuteSystems(
				registryId: defReg.registryId,
				executionCount: 1,
				new EcsactRuntime.ExecutionOptions[]{defReg.executionOptions}
			);

#if UNITY_EDITOR
			executionTimeWatch.Stop();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif

			defReg.executionOptions = new EcsactRuntime.ExecutionOptions();
		}
	}
}
