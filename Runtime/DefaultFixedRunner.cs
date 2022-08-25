using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class DefaultFixedRunner : EcsactRunner {

		[RuntimeInitializeOnLoadMethod]
		private static void OnRuntimeLoad() {
			EcsactRunner.OnRuntimeLoad<DefaultFixedRunner>(
				EcsactRuntimeDefaultRegistry.RunnerType.FixedUpdate,
				"Default Fixed Runner"
			);
		}

		void FixedUpdate() {
			if(defReg == null) return;
			UnityEngine.Debug.Assert(defReg.registryId != -1);

#if UNITY_EDITOR
			var executionTimeWatch = Stopwatch.StartNew();
#endif

			AddActionsToReg();
			try {
				runtimeInstance!.core.ExecuteSystems(
					registryId: defReg.registryId,
					executionCount: 1,
					new EcsactRuntime.ExecutionOptions[]{defReg.executionOptions}
				);
			} finally {
				FreeActions();
			}

#if UNITY_EDITOR
			executionTimeWatch.Stop();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif

			defReg.executionOptions = new EcsactRuntime.ExecutionOptions();
		}
	}
}
