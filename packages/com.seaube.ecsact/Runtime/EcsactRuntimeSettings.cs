using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

[System.Serializable]
public class EcsactRuntimeSettings : ScriptableObject {
	private static EcsactRuntimeSettings? _instance;

	public const string resourcePath = "Settings/EcsactRuntimeSettings.asset";
	public const string assetPath = "Assets/Resources/" + resourcePath;

	public List<string> runtimeLibraryPaths = new();

	public static EcsactRuntimeSettings Get() {
		if(_instance != null) return _instance;

#if UNITY_EDITOR
		_instance = AssetDatabase.LoadAssetAtPath<EcsactRuntimeSettings>(
			assetPath
		);
		if(_instance == null) {
			_instance = ScriptableObject.CreateInstance<EcsactRuntimeSettings>();
			Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
			AssetDatabase.CreateAsset(_instance, assetPath);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}
#else
		_instance = Resources.Load(resourcePath) as EcsactRuntimeSettings;
#endif

		return _instance;
	}
}
