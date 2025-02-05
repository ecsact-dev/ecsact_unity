using UnityEngine;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class AsyncRunner : EcsactRunner {
	private EcsactRuntime? runtime;

	public global::System.Int32? SessionId;

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
				SessionId.Value!,
				localExecutionOptions.C()
			);
		} finally {
			executionOptions.Free();
		}
	}

	void Update() {
		if(!SessionId.HasValue) {
			return;
		}

		if(Ecsact.Defaults.Runtime != null) {
			if(!executionOptions.isEmpty()) {
				Enqueue();
			}
			Ecsact.Defaults.Runtime.async.Flush(SessionId.Value);
		}
	}
}

} // namespace Ecsact
