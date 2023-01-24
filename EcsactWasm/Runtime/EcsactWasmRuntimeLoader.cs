using System;
using System.IO;
using System.Collections.Generic;
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

		Ecsact.Defaults.WhenReady(OnRuntimeReady);
	}

	private static void OnRuntimeReady() {
		var settings = EcsactWasmRuntimeSettings.Get();
#	if UNITY_EDITOR
		StartWatcher(settings);
#	endif
		Load(settings);
	}

#	if UNITY_EDITOR
	private static Dictionary<string, FileSystemWatcher> _watchers = new();

	[UnityEditor.InitializeOnLoadMethod]
	private static void OnLoadEditor() {
		UnityEditor.EditorApplication.playModeStateChanged +=
			OnPlayModeStateChanged;
	}

	private static void ClearWatchers() {
		foreach(var entry in _watchers) {
			entry.Value.Dispose();
		}
		_watchers.Clear();
	}

	private static void OnPlayModeStateChanged(
		UnityEditor.PlayModeStateChange state
	) {
		if(state == UnityEditor.PlayModeStateChange.ExitingEditMode) {
			ClearWatchers();
		}
	}

	private static void StartWatcher(EcsactWasmRuntimeSettings settings) {
		foreach(var entry in settings.wasmSystemEntries) {
			if(!entry.wasmAsset) continue;
			var assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.wasmAsset);
			if(String.IsNullOrEmpty(assetPath)) {
				Debug.LogWarning(
					$"Cannot find wasm asset path. Hot reloading wasm system impl is disabled for {entry.wasmExportName}"
				);
				continue;
			}

			var assetDirectory = Path.GetDirectoryName(assetPath);
			if(_watchers.ContainsKey(assetDirectory)) {
				continue;
			}

			var watcher = new FileSystemWatcher(assetDirectory);
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += OnWatcherChangedEvent;
			watcher.EnableRaisingEvents = true;

			_watchers.Add(assetDirectory, watcher);
		}
	}

	private static void ReloadWasmFile(string filePath) {
		var settings = EcsactWasmRuntimeSettings.Get();
		UnityEngine.Debug.Log($"Reloading wasm system impls {filePath}");
		foreach(var entry in settings.wasmSystemEntries) {
			if(string.IsNullOrWhiteSpace(entry.wasmExportName)) {
				Debug.Log("no export name...", entry.wasmAsset);
				continue;
			}
			var assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.wasmAsset);
			if(String.IsNullOrEmpty(assetPath)) {
				Debug.Log("No asset path...", entry.wasmAsset);
				continue;
			}

			if(Path.GetFullPath(assetPath) == filePath) {
				Debug.Log($"  Reloading {entry.wasmExportName}", entry.wasmAsset);

				var loadError = Ecsact.Defaults.Runtime.wasm.LoadFile(
					filePath,
					entry.systemId,
					entry.wasmExportName
				);
				PrintLoadError(loadError, entry.wasmExportName, entry.wasmAsset);
			} else {
				Debug.Log(
					$"Skipping: {Path.GetFullPath(assetPath)} != {filePath}",
					entry.wasmAsset
				);
			}
		}
	}

	private static void OnWatcherChangedEvent(object _, FileSystemEventArgs ev) {
		var fileExt = Path.GetExtension(ev.FullPath);
		if(fileExt != ".wasm") return;

		var wasmFilePath = ev.FullPath;
		UnityEditor.EditorApplication.delayCall += () => {
			if(Application.isPlaying) {
				ReloadWasmFile(wasmFilePath);
			}
		};
	}
#	endif

	private static void Load(EcsactWasmRuntimeSettings settings) {
		foreach(var entry in settings.wasmSystemEntries) {
			if(string.IsNullOrWhiteSpace(entry.wasmExportName)) continue;

			var loadError = Ecsact.Defaults.Runtime.wasm.Load(
				entry.wasmAsset.bytes,
				entry.systemId,
				entry.wasmExportName
			);

			PrintLoadError(loadError, entry.wasmExportName, entry.wasmAsset);
		}
	}

	private static void PrintLoadError(
		EcsactRuntime.Wasm.Error err,
		string                   exportName,
		UnityEngine.Object       context = null
	) {
		if(err.code != EcsactRuntime.Wasm.ErrorCode.Ok) {
			var errMessage = $"Failed to load ecsact Wasm system impl. " +
				$"ErrorCode={err.code} ExportName={exportName}";

			if(err.message.Length > 0) {
				errMessage += $": {err.message}";
			}

			UnityEngine.Debug.LogError(errMessage, context);
		}
	}
}

} // namespace Ecsact

#endif // HAS_UNITY_WASM_PACKAGE
