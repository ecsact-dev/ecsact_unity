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
		// NOTE(Kelwan): Elaborate on C# systems not working with Async currently
		// The async runner's system execution is not in C# land
		var ownerPinned = GCHandle.Alloc(runtime, GCHandleType.Pinned);
		try {
			var ownerIntPtr = GCHandle.ToIntPtr(ownerPinned);
			runtime._execEvs.initCallbackUserData = ownerIntPtr;
			runtime._execEvs.updateCallbackUserData = ownerIntPtr;
			runtime._execEvs.removeCallbackUserData = ownerIntPtr;

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
				Enqueue();
			}

			// runtime.async.Flush();
		}
	}
}

} // namespace Ecsact
