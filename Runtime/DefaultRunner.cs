using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class DefaultRunner : EcsactRunner {

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
