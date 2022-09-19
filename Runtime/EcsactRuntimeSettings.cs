using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
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

#if UNITY_EDITOR
	public static event Action<EcsactRuntimeSettings>? editorValidateEvent;
#endif

	// Turned off for now.
	// @SEE: https://github.com/ecsact-dev/ecsact_unity/issues/20
	[NonSerialized]
	public bool useAsyncRunner = false;
	public bool useVisualScriptingEvents = true;
	[Tooltip(
		"List of ecsact registries that are created automatically when " +
		"the game loads."
	)]
	public List<EcsactRuntimeDefaultRegistry> defaultRegistries = new();
	public bool useUnitySync = false;
	/*
		NOTE(Kelwan): The constraints on this list are ambiguous. We should
		eventually have a strict type that can be visible in the editor
	**/
	public List<string> unitySyncScripts = new();

	[Tooltip(
		"Path to ecsact runtime library. First element is always the generated " +
		"runtime from the runtime builder."
	)]
	public List<string> runtimeLibraryPaths = new();

#if UNITY_EDITOR
	void OnValidate() {
		if(defaultRegistries.Count > 1) {
			throw new Exception(
				"Only 1 registry is supported (multiple will be supported in a later update"
			);
		}

		if(defaultRegistries.Count == 0) {
			defaultRegistries.Add(new EcsactRuntimeDefaultRegistry{
				registryName = "Default Registry",
				runner = EcsactRuntimeDefaultRegistry.RunnerType.FixedUpdate,
			});
		}

		if(editorValidateEvent != null) {
			editorValidateEvent.Invoke(this);
		}
	}
#endif

	public static EcsactRuntimeSettings Get() {
		if(_instance != null) {
			return _instance;
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
