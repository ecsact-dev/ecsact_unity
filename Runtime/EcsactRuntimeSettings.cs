using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

using Ecsact.UnitySync;

[System.Serializable]
public class EcsactRuntimeDefaultRegistry {
	public enum RunnerType {
		None,
		Update,
		FixedUpdate,
	}

	public EntityGameObjectPool? pool;

	[Tooltip("Name given to registry. For display and debug purposes only")]
	public string registryName = "";
	[Tooltip(
		"Create game object at startup with the Ecsact.DefaultRunner or " +
		"Ecsact.DefaultFixedRunner script for this registry."
	)]
	public RunnerType runner;
	public EcsactRuntime.ExecutionOptions executionOptions;
	public Int32 registryId { get; internal set; } = -1;
}

[System.Serializable]
public class EcsactRuntimeSettings : ScriptableObject {
	private static EcsactRuntimeSettings? _instance;

	public const string resourcePath = "Settings/EcsactRuntimeSettings";
	public const string assetPath = "Assets/Resources/" + resourcePath + ".asset";

	public bool useAsyncRunner = true;
	public bool useVisualScriptingEvents = true;
	[Tooltip(
		"List of ecsact registries that are created automatically when " +
		"the game loads."
	)]
	public List<EcsactRuntimeDefaultRegistry> defaultRegistries = new();
	public bool useUnitySync = false;
	// [ShowIf("_instance.useUnitySync", true)]
	public List<string> unitySyncScripts = new();
	public List<string> runtimeLibraryPaths = new();

	public static EcsactRuntimeSettings Get() {
		if(_instance != null) {
			return _instance;
		}

	void OnValidate() {
		if(_instance.defaultRegistries.Count > 1) {
			throw new Exception(
				"Only 1 registry is supported (multiple will be supported in a later update"
			);
		}
	}

#if UNITY_EDITOR
		_instance = AssetDatabase.LoadAssetAtPath<EcsactRuntimeSettings>(assetPath);
		if(_instance == null) {
			_instance = ScriptableObject.CreateInstance<EcsactRuntimeSettings>();
			Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
			AssetDatabase.CreateAsset(_instance, assetPath);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}
#else
		_instance = Resources.Load(resourcePath) as EcsactRuntimeSettings;
#endif

		if(_instance == null) {
			throw new Exception("Failed to load ecsact runtime settings");
		}

		return _instance;
	}
}
