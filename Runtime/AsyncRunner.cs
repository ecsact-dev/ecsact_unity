using UnityEngine;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class AsyncRunner : EcsactRunner {
	private static AsyncRunner? instance = null;
	private EcsactRuntime? runtime;

	private static void OnRuntimeLoad() {
		if(instance != null) {
			return;
		}

		var settings = EcsactRuntimeSettings.Get();
		if(!settings) {
			return;
		}

		Ecsact.Defaults.WhenReady(() => {
			var gameObject = new GameObject("Ecsact Async Runner");
			instance = gameObject.AddComponent<AsyncRunner>();
			instance.runtime = Ecsact.Defaults.Runtime;
			DontDestroyOnLoad(gameObject);
		});
	}

	void Connect() {
		runtime.async.Connect("someConnectStr");
	}

	void Disconnect() {
		runtime.async.Disconnect();
	}

	private void Enqueue() {
		// NOTE(Kelwan): Elaborate on C# systems not working with Async currently
		var ownerPinned =
			GCHandle.Alloc(Ecsact.Defaults.Runtime, GCHandleType.Pinned);
		try {
			var ownerIntPtr = GCHandle.ToIntPtr(ownerPinned);
			Ecsact.Defaults.Runtime._execEvs.initCallbackUserData = ownerIntPtr;
			Ecsact.Defaults.Runtime._execEvs.updateCallbackUserData = ownerIntPtr;
			Ecsact.Defaults.Runtime._execEvs.removeCallbackUserData = ownerIntPtr;

			executionOptions.Alloc();
			runtime.async.EnqueueExecutionOptions(executionOptions.C());
			executionOptions.Free();
		} finally {
			ownerPinned.Free();
		}
	}

	void Update() {
		// Take execution events ideas from ExecuteSystems
		// Send in callbacks so it works with Unity sync
		// Adds IsEmpty for executionOptions
		// EX:
		if(!executionOptions.isEmpty()) {
			Enqueue();
		}

		runtime.async.Flush();
	}

	void OnDestroy() {
		if(this.Equals(instance)) {
			instance = null;
		}
	}
}

} // namespace Ecsact
