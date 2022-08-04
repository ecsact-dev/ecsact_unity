using System;
using UnityEngine;

#if HAS_UNITY_WASM_PACKAGE

namespace Ecsact {
	public static class EcsactWasmRuntimeLoader {
		[RuntimeInitializeOnLoadMethod]
		public static void OnLoad() {
			var settings = EcsactWasmRuntimeSettings.Get();
			if(!settings.useDefaultLoader) {
				return;
			}

			var defaultRuntime = EcsactRuntime.GetOrLoadDefault();
			// defaultRuntime.wasm.

			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach(var type in assembly.GetTypes()) {
					if(Util.IsSystem(type) || Util.IsAction(type)) {
						Debug.Log($"System {type.Name} ID is {Util.GetSystemID(type)}");
					}
				}
			}

			foreach(var entry in settings.wasmSystemEntries) {
				if(string.IsNullOrWhiteSpace(entry.wasmExportName)) continue;

				Debug.Log($"Loading {entry.wasmExportName}");
				defaultRuntime.wasm.Load(
					entry.wasmAsset.bytes,
					entry.systemId,
					entry.wasmExportName
				);
			}
		}
	}
}

#endif // HAS_UNITY_WASM_PACKAGE
