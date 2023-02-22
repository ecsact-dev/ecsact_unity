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

	private Ecsact.Details.ExecutionEntityCallbacks entityCallbacks = new();

	void Start() {
		Ecsact.Defaults.Runtime.OnEntityCreated((entityId, placeholderId) => {
			var callback = entityCallbacks.GetAndClearCallback(placeholderId);
			callback(entityId);
		});
	}

	public Ecsact.ExecutionOptions executionOptions = new();

	internal static EcsactRunner CreateInstance<ComponentT>(
		EcsactRuntimeDefaultRegistry.RunnerType runnerType,
		EcsactRuntimeSettings                   settings,
		string                                  name
	)
		where ComponentT : EcsactRunner {
		var gameObjectName = name;

		var gameObject = new GameObject(gameObjectName);
		var runner = gameObject.AddComponent(typeof(ComponentT)) as EcsactRunner;
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
		if(!executionOptions.isEmpty()) {
			executionOptions.Alloc();
		}

		try {
			LoadEntityCallbacks();
			// NOTE: Temporary, this should be abstracted out
			executionOptions.executionOptions.createEntities =
				executionOptions.create_entities_placeholders.ToArray();
			Ecsact.Defaults.Registry.ExecuteSystems(executionOptions);
		} finally {
			executionOptions.Free();
#if UNITY_EDITOR
			executionTimeWatch.Stop();
			executionOptions.create_entities_placeholders = new();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif

			executionOptions.executionOptions = new EcsactRuntime.CExecutionOptions();
		}

		Ecsact.Defaults.Runtime.wasm.PrintAndConsumeLogs();
	}

	protected void LoadEntityCallbacks() {
		for(int i = 0; i < executionOptions.create_entities.Count; i++) {
			var builder = executionOptions.create_entities[i];
			var id = entityCallbacks.AddCallback(builder.callback);
			// NOTE: Temporary, this should be abstracted out
			executionOptions.create_entities_placeholders.Add(id);
		}
	}
}

} // namespace Ecsact
