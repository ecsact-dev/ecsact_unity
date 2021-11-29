using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;

class EcsIdlSettings : ScriptableObject {
	public const string assetPath = "Assets/Editor/EcsIdlSettings.asset";
	public const string path = "Project/EcsIdlProjectSettings";
	public const SettingsScope scope = SettingsScope.Project;

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
				"Runtime",
				"Library",
			}),
		};
	}
}
