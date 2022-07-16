using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class DefaultFixedRunner : MonoBehaviour {
		private EcsactRuntime? runtimeInstance = null;

		private EcsactRuntimeDefaultRegistry? defReg;

		public Int32 registryId => defReg?.registryId ?? -1;

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
				if(defReg.runner != EcsactRuntimeDefaultRegistry.RunnerType.FixedUpdate) {
					continue;
				}

				var gameObjectName = $"Ecsact Default Fixed Runner";
				if(!string.IsNullOrWhiteSpace(defReg.registryName)) {
					gameObjectName += " - " + defReg.registryName;
				}

				var gameObject = new GameObject(gameObjectName);
				var runner = gameObject.AddComponent<DefaultFixedRunner>();
				runner.defReg = defReg;
				DontDestroyOnLoad(gameObject);
			}
		}

		void Awake() {
			runtimeInstance = EcsactRuntime.GetOrLoadDefault();
		}

		void Start() {
			gameObject.name += $" ({defReg!.registryId})";
		}

		void FixedUpdate() {
			if(defReg == null) return;
			UnityEngine.Debug.Assert(defReg.registryId != -1);

#if UNITY_EDITOR
			var executionTimeWatch = Stopwatch.StartNew();
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
