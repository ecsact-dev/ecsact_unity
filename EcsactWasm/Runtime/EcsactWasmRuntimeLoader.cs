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

			foreach(var entry in settings.wasmSystemEntries) {
				if(string.IsNullOrWhiteSpace(entry.wasmExportName)) continue;

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
