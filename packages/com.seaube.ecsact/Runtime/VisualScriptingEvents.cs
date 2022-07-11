using UnityEngine;
using Unity.VisualScripting;
using System;
using System.Collections.Generic;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class VisualScriptingEvents : MonoBehaviour {
		private static VisualScriptingEvents? instance = null;
		private EcsactRuntime? runtimeInstance = null;

		[RuntimeInitializeOnLoadMethod]
		private static void OnRuntimeLoad() {
			if(instance != null) {
				return;
			}

			var settings = EcsactRuntimeSettings.Get();
			if(!settings.useVisualScriptingEvents) {
				return;
			}

			var gameObject = new GameObject("Ecsact Visual Scripting Events");
			instance = gameObject.AddComponent<VisualScriptingEvents>();
			DontDestroyOnLoad(gameObject);
		}

		List<System.Action> diposeCallbacks = new();

		void OnEnable() {
			runtimeInstance = EcsactRuntime.GetOrLoadDefault();
			// diposeCallbacks.Add(runtimeInstance!.@async.OnError((err, reqId) => {
			// 	Unity.VisualScripting.EventBus.Trigger(
			// 		EcsactRuntime.VisualScriptingEventNames.AsyncError,
			// 		err
			// 	);
			// }));
		}

		void OnDisable() {
			foreach(var diposeCb in diposeCallbacks) {
				diposeCb();
			}
			diposeCallbacks.Clear();
		}
	}
}
