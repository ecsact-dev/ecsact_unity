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
		Ecsact.Defaults.WhenReady(() => {
			Ecsact.Defaults.Runtime.OnEntityCreated((entityId, placeholderId) => {
				EcsactRuntime.EntityIdCallback callback;

				var hasCallback =
					entityCallbacks.GetAndClearCallback(placeholderId, out callback);
				if(hasCallback) {
					callback(entityId);
				}
			});
		});
	}

	public Ecsact.ExecutionOptions executionOptions = new();

	internal static EcsactRunner CreateInstance<ComponentT>(
		EcsactRuntimeSettings settings,
		string                name
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

		var localExecutionOptions = executionOptions;
		try {
			executionOptions = new();
			LoadEntityCallbacks(localExecutionOptions);
			localExecutionOptions.executionOptions.createEntities =
				localExecutionOptions.create_entities_placeholders.ToArray();
			Ecsact.Defaults.Registry.ExecuteSystems(localExecutionOptions);
		} finally {
			localExecutionOptions.Free();
#if UNITY_EDITOR
			executionTimeWatch.Stop();
			localExecutionOptions.create_entities_placeholders = new();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif
		}

		Ecsact.Defaults.Runtime.wasm.PrintAndConsumeLogs();
	}

	protected void LoadEntityCallbacks(ExecutionOptions localExecutionOptions) {
		for(int i = 0; i < localExecutionOptions.create_entities.Count; i++) {
			var builder = localExecutionOptions.create_entities[i];
			var id = entityCallbacks.AddCallback(builder.callback);
			localExecutionOptions.create_entities_placeholders.Add(id);
		}
	}
}

} // namespace Ecsact
