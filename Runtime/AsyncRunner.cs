using UnityEngine;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class AsyncRunner : EcsactRunner {
	private EcsactRuntime? runtime;

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
				localExecutionOptions.C()
			);
		} finally {
			executionOptions.Free();
		}
	}

	void Update() {
		if(Ecsact.Defaults.Runtime != null) {
			if(!executionOptions.isEmpty()) {
				Enqueue();
			}
			Ecsact.Defaults.Runtime.async.Flush();
		}
	}
}

} // namespace Ecsact
