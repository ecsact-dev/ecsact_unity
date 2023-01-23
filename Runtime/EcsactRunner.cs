using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

[assembly:InternalsVisibleTo("EcsactRuntimeDefaults")]

namespace Ecsact {

[AddComponentMenu("")]
public class EcsactRunner : MonoBehaviour {
#if UNITY_EDITOR
	[NonSerialized]
	public int debugExecutionCountTotal = 0;
	[NonSerialized]
	public int debugExecutionTimeMs = 0;
#endif

	public Ecsact.ExecutionOptions executionOptions = new();

	internal static EcsactRunner CreateInstance<ComponentT>(
		EcsactRuntimeSettings settings,
		string                name
	)
		where ComponentT : EcsactRunner {
		var gameObjectName = name;

		var gameObject = new GameObject(gameObjectName);
		var runner = gameObject.AddComponent(typeof(ComponentT)) as EcsactRunner;

		//

		if(runner is not null) {
			runner.executionOptions = new Ecsact.ExecutionOptions();
			DontDestroyOnLoad(gameObject);
		} else {
			throw new Exception("Runner is not valid");
		}

		return runner;
	}

	protected void Execute() {
#if UNITY_EDITOR
		var executionTimeWatch = Stopwatch.StartNew();
#endif
		executionOptions.Alloc();
		try {
			Ecsact.Defaults.Registry.ExecuteSystems(executionOptions);
		} finally {
			executionOptions.Free();
#if UNITY_EDITOR
			executionTimeWatch.Stop();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif

			executionOptions.executionOptions = new EcsactRuntime.CExecutionOptions();
		}

		Ecsact.Defaults.Runtime.wasm.PrintAndConsumeLogs();
	}
}

} // namespace Ecsact
