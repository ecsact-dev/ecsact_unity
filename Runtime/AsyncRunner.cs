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
		var ownerPinned = GCHandle.Alloc(runtime, GCHandleType.Pinned);
		try {
			var ownerIntPtr = GCHandle.ToIntPtr(ownerPinned);
			runtime._execEvs.initCallbackUserData = ownerIntPtr;
			runtime._execEvs.updateCallbackUserData = ownerIntPtr;
			runtime._execEvs.removeCallbackUserData = ownerIntPtr;

			LoadEntityCallbacks();

			executionOptions.Alloc();
			runtime.async.EnqueueExecutionOptions(executionOptions.C());
			executionOptions.Free();
		} finally {
			ownerPinned.Free();
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
