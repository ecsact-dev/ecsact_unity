using UnityEngine;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class AsyncRunner : EcsactRunner {
	private EcsactRuntime? runtime;

	public global::System.Int32? sessionId;

	private void Enqueue() {
		var localExecutionOptions = executionOptions;

		try {
			executionOptions = new();
			LoadEntityCallbacks(localExecutionOptions);
			// NOTE: Temporary, this should be abstracted out
			// Everything involving create_entities_placeholders should go elsewhere
			localExecutionOptions.executionOptions.createEntities =
				localExecutionOptions.create_entities_placeholders.ToArray();
			localExecutionOptions.Alloc();
			Ecsact.Defaults.Runtime.async.EnqueueExecutionOptions(
				sessionId!.Value,
				localExecutionOptions.C()
			);
		} finally {
			executionOptions.Free();
		}
	}

	void Update() {
		if(!sessionId.HasValue) {
			return;
		}

		if(Ecsact.Defaults.Runtime != null) {
			if(!executionOptions.isEmpty()) {
				Enqueue();
			}
			Ecsact.Defaults.Runtime.async.Flush(sessionId.Value);
		}
	}
}

} // namespace Ecsact
