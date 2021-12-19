using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
class EcsIdlSettings : ScriptableObject {
	public const string assetPath = "Assets/Editor/EcsIdlSettings.asset";
	public const string path = "Project/EcsIdlProjectSettings";
	public const SettingsScope scope = SettingsScope.Project;

	/// <summary>Path to <c>ecs_idl_parser_info_codegen.exe</c>. If unset
	/// the latest will be downloaded and used instead.</summary>
	public string parseInfoCodegenPluginPath = "";

	/// <summary>Path to <c>ecs_idl_csharp_codegen.exe</c>. If unset the latest
	/// will be downloaded and used instead.</summary>
	public string csharpCodegenPluginPath = "";

	internal static EcsIdlSettings GetOrCreateSettings() {
		var settings = AssetDatabase.LoadAssetAtPath<EcsIdlSettings>(assetPath);
		if(settings == null) {
			settings = ScriptableObject.CreateInstance<EcsIdlSettings>();
			AssetDatabase.CreateAsset(settings, assetPath);
			AssetDatabase.SaveAssetIfDirty(settings);
		}

		return settings;
	}

	internal static SerializedObject GetSerializedSettings() {
		return new SerializedObject(GetOrCreateSettings());
	}
}

static class EcsIdlSettingsUIElementsRegister {
	[SettingsProvider]
	public static SettingsProvider CreateEcsIdlSettingsProvider() {
		return new SettingsProvider(EcsIdlSettings.path, EcsIdlSettings.scope) {
			label = "ECS IDL",
			activateHandler = (searchContext, rootElement) => {
				var settings = EcsIdlSettings.GetSerializedSettings();
				var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					"Packages/com.seaube.ecs-idl/Editor/EcsIdlSettings.uxml"
				);
				var ui = template.Instantiate();
				BindingExtensions.Bind(ui, settings);
				rootElement.Add(ui);
			},
			keywords = new HashSet<string>(new[] {
				"ECS IDL",
				"ECS-IDL",
				"ECS",
				"ECS Plugin",
				"Plugin",
				"Runtime",
				"Library",
			}),
		};
	}
}
