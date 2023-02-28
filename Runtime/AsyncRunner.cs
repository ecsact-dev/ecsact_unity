using UnityEngine;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class AsyncRunner : EcsactRunner {
	private EcsactRuntime? runtime;
	private int? tickRate;

	void Start() {
		Ecsact.Defaults.WhenReady(() => { runtime = Ecsact.Defaults.Runtime; });
	}

	private void Enqueue() {
		var localExecutionOptions = executionOptions;

		try {
			executionOptions = new();
			LoadEntityCallbacks(localExecutionOptions);
			// NOTE: Temporary, this should be abstracted out
			localExecutionOptions.executionOptions.createEntities =
				localExecutionOptions.create_entities_placeholders.ToArray();
			localExecutionOptions.Alloc();
			runtime.async.EnqueueExecutionOptions(localExecutionOptions.C());
		} finally {
			executionOptions.Free();
		}
	}

	void Update() {
		if(runtime != null) {
			if(!executionOptions.isEmpty()) {
				UnityEngine.Debug.Log("Enqueue");
				Enqueue();
			}
			runtime.async.Flush();
		}
	}
}

} // namespace Ecsact
