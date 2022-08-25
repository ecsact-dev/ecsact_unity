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
			Execute();
		}
	}
}
