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
			Execute();
		}
	}
}
