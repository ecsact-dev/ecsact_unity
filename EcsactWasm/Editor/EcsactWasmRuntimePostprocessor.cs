using UnityEngine;
using UnityEditor;
using System.IO;

#if HAS_UNITY_WASM_PACKAGE

public class EcsactWasmRuntimePostprocessor : AssetPostprocessor {
	static void OnPostprocessAllAssets
		( string[]  importedAssets
		, string[]  deletedAssets
		, string[]  movedAssets
		, string[]  movedFromAssetPaths
		)
	{
		if(importedAssets.Length == 0 && deletedAssets.Length == 0) {
			return;
		}

		var settings = EcsactWasmRuntimeSettings.Get();
		if(!settings.autoFindSystemImpls) {
			return;
		}

		foreach(var importedAsset in importedAssets) {
			if(Path.GetExtension(importedAsset) == ".wasm") {
				AutoUpdateWasmSystemImpls();
				return;
			}

			if(Path.GetExtension(importedAsset) == ".ecsact") {
				AutoUpdateWasmSystemImpls();
				return;
			}
		}

		foreach(var deletedAsset in deletedAssets) {
			if(Path.GetExtension(deletedAsset) == ".wasm") {
				AutoUpdateWasmSystemImpls();
				return;
			}

			if(Path.GetExtension(deletedAsset) == ".ecsact") {
				AutoUpdateWasmSystemImpls();
				return;
			}
		}
	}

	static void AutoUpdateWasmSystemImpls() {
		EcsactWasmRuntimeSettingsEditor.FindSystemImpls(settings => {
			EditorUtility.SetDirty(settings);
		});
	}
}

#endif // HAS_UNITY_WASM_PACKAGE
