using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

#if HAS_UNITY_WASM_PACKAGE

[System.Serializable]
public class EcsactWasmRuntimeSettings : ScriptableObject {
	[System.Serializable]
	public class SystemMapEntry {
		public Int32 systemId = -1;
		public WasmAsset? wasmAsset;
		public string wasmExportName = "";
	}

	private static EcsactWasmRuntimeSettings? _instance;

	public const string resourcePath = "Settings/EcsactWasmRuntimeSettings";
	public const string assetPath = "Assets/Resources/" + resourcePath + ".asset";

	public bool useDefaultLoader = true;
	public List<SystemMapEntry> wasmSystemEntries = new();

	public static EcsactWasmRuntimeSettings Get() {
		if(_instance != null) {
			return _instance;
		}

#if UNITY_EDITOR
		_instance = AssetDatabase.LoadAssetAtPath<EcsactWasmRuntimeSettings>(
			assetPath
		);
		if(_instance == null) {
			_instance = ScriptableObject.CreateInstance<EcsactWasmRuntimeSettings>();
			Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
			AssetDatabase.CreateAsset(_instance, assetPath);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}
#else
		_instance = Resources.Load(resourcePath) as EcsactWasmRuntimeSettings;
#endif

		if(_instance == null) {
			throw new Exception("Failed to load ecsact wasm runtime settings");
		}

		return _instance;
	}
}

#endif // HAS_UNITY_WASM_PACKAGE
