using System;
using UnityEngine;

#if HAS_UNITY_WASM_PACKAGE

namespace Ecsact {
	public static class EcsactWasmRuntimeLoader {
		public static bool Enabled() {
			var settings = EcsactRuntimeSettings.Get();
			if(settings.systemImplSource != Ecsact.SystemImplSource.WebAssembly) {
				return false;
			}

			return true;
		}

		[RuntimeInitializeOnLoadMethod]
		public static void OnLoad() {
			if(!Enabled()) {
				return;
			}

			Ecsact.Defaults.WhenReady(() => {
				Load();
			});
		}

		private static void Load() {
			var settings = EcsactWasmRuntimeSettings.Get();
			foreach(var entry in settings.wasmSystemEntries) {
				if(string.IsNullOrWhiteSpace(entry.wasmExportName)) continue;

				var loadError = Ecsact.Defaults._Runtime.wasm.Load(
					entry.wasmAsset.bytes,
					entry.systemId,
					entry.wasmExportName
				);

				if(loadError != EcsactRuntime.Wasm.Error.Ok) {
					UnityEngine.Debug.LogError(
						$"Failed to load ecsact Wasm system impl. " +
						$"ErrorCode={loadError} ExportName={entry.wasmExportName}"
					);
				}
			}
		}
	}
}

#endif // HAS_UNITY_WASM_PACKAGE
