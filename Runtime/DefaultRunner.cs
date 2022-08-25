using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class DefaultRunner : EcsactRunner {

#if UNITY_EDITOR
		[NonSerialized]
		public int debugExecutionCountTotal = 0;
		[NonSerialized]
		public int debugExecutionTimeMs = 0;
#endif

		[RuntimeInitializeOnLoadMethod]
		private static void OnRuntimeLoad() {
			EcsactRunner.OnRuntimeLoad<DefaultRunner>(
				EcsactRuntimeDefaultRegistry.RunnerType.Update,
				"Default Runner"
			);
		}

		void Update() {
			if(defReg == null) return;
			UnityEngine.Debug.Assert(defReg.registryId != -1);

#if UNITY_EDITOR
			var executionTimeWatch = Stopwatch.StartNew();
#endif

			var actionArray = actionList.ToArray();
			defReg.executionOptions.actions = actionArray;
			defReg.executionOptions.actionsLength = actionArray.Length;

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

			actionList.Clear();
			defReg.executionOptions = new EcsactRuntime.ExecutionOptions();
		}
	}
}
