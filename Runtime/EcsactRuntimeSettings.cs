using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Ecsact.UnitySync;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

[System.Serializable]
public class EcsactRuntimeDefaultRegistry {
	public enum UpdateMethod {
		None,
		Update,
		FixedUpdate,
	}

	[NonSerialized]
	public EntityGameObjectPool? pool;

	[Tooltip("Name given to registry. For display and debug purposes only")]
	public string registryName = "";
	[Tooltip(
		"Create game object at startup with the Ecsact.DefaultRunner or " +
		"Ecsact.DefaultFixedRunner script for this registry."
	)]
	public UpdateMethod updateMethod;
	public Int32        registryId { get; internal set; } = -1;
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
	// NOTE: Close issue on completion
	public enum RunnerType {
		DefaultRunner,
		AsyncRunner,
	}

	public bool useVisualScriptingEvents = true;
	[Tooltip(
		"List of ecsact registries that are created automatically when " +
		"the game loads."
	)]

	public RunnerType runner;

	[HideInInspector]
	public int tickRate = 32;

	[HideInInspector]
	public EcsactRuntimeDefaultRegistry defaultRegistry =
		new EcsactRuntimeDefaultRegistry {
			registryName = "Default Registry",
			updateMethod = EcsactRuntimeDefaultRegistry.UpdateMethod.FixedUpdate,
		};

	[System.Serializable]
	public struct UnitySyncScriptInfo {
		public bool   scriptEnabled;
		public string scriptAssemblyQualifiedName;
	}

	[HideInInspector]
	public bool enableUnitySync = false;
	/// List of Ecsact Unity Sync scripts to be registered on load
	[HideInInspector]
	public List<UnitySyncScriptInfo> unitySyncScripts = new();

	[System.Serializable]
	public struct CsharpSystemImplInfo {
		public bool   enabled;
		public string assemblyQualifiedName;
		public string implMethodName;
	}

	[HideInInspector]
	public Ecsact.SystemImplSource systemImplSource;

	[HideInInspector]
	public string defaultCsharpSystemImplsAssemblyName = "";

	[Tooltip(
		"Path to ecsact runtime library. First element is always the generated " +
		"runtime from the runtime builder."
	)]
	public List<string> runtimeLibraryPaths = new();

#if UNITY_EDITOR
	void OnValidate() {
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
