using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using Ecsact.Editor.Internal;

#nullable enable

[System.Serializable]
class EcsactSettings : ScriptableObject {
	public const string assetPath = "Assets/Editor/EcsactSettings.asset";
	public const string path = "Project/EcsactProjectSettings";
	public const SettingsScope scope = SettingsScope.Project;

	/// <summary>Path to <c>ecsact_parser_info_codegen.exe</c>. If unset
	/// the latest will be downloaded and used instead.</summary>
	public string parseInfoCodegenPluginPath = "";

	/// <summary>Path to <c>ecsact_csharp_codegen.exe</c>. If unset the latest
	/// will be downloaded and used instead.</summary>
	public string csharpCodegenPluginPath = "";

	public string runtimeBuilderOutputPath = "Assets/Plugins/EcsactRuntime.dll";

	public string runtimeBuilderCompilerPath = "";

	public static EcsactSettings GetOrCreateSettings() {
		var settings = AssetDatabase.LoadAssetAtPath<EcsactSettings>(assetPath);
		if(settings == null) {
			settings = ScriptableObject.CreateInstance<EcsactSettings>();
			AssetDatabase.CreateAsset(settings, assetPath);
			AssetDatabase.SaveAssetIfDirty(settings);
		}

		return settings;
	}

	internal static SerializedObject GetSerializedSettings() {
		return new SerializedObject(GetOrCreateSettings());
	}
}

[System.Serializable]
class EcsactMethodUIBindings : ScriptableObject {
	public string methodName = "";
	public bool methodLoaded = false;
}

static class EcsactSettingsUIElementsRegister {

	internal static void SetupMethodsUI
		( TemplateContainer    ui
		, string               moduleName
		, IEnumerable<string>  methods
		, IEnumerable<string>  availableMethods
		)
	{
		var coreMethodTemplate = ui.Q<TemplateContainer>(
			$"{moduleName}-method-template"
		);

		foreach(var method in methods) {
			var clone = coreMethodTemplate.templateSource.CloneTree();
			var bindings =
				ScriptableObject.CreateInstance<EcsactMethodUIBindings>();
			bindings.methodName = method;
			bindings.methodLoaded = availableMethods.Contains(method);
			BindingExtensions.Bind(clone, new SerializedObject(bindings));

			if(bindings.methodLoaded) {
				var missingIcon = clone.Q<VisualElement>("method-missing-icon");
				missingIcon.style.display = DisplayStyle.None;
			} else {
				var availableIcon = clone.Q<VisualElement>("method-available-icon");
				availableIcon.style.display = DisplayStyle.None;
			}

			coreMethodTemplate.contentContainer.parent.Add(clone);
		}

		coreMethodTemplate.contentContainer.style.display = DisplayStyle.None;
	}

	[SettingsProvider]
	public static SettingsProvider CreateEcsactSettingsProvider() {
		EcsactRuntime? testDefaultRuntime = null;
		Editor? runtimeSettingsEditor = null;
		Editor? wasmRuntimeSettingsEditor = null;

		return new SettingsProvider(EcsactSettings.path, EcsactSettings.scope) {
			label = "Ecsact",
			activateHandler = (searchContext, rootElement) => {
				var settings = EcsactSettings.GetSerializedSettings();
				var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					"Packages/com.seaube.ecsact/Editor/EcsactSettings.uxml"
				);
				var ui = template.Instantiate();
				BindingExtensions.Bind(ui, settings);
				rootElement.Add(ui);

				if(testDefaultRuntime != null) {
					EcsactRuntime.Free(testDefaultRuntime);
					testDefaultRuntime = null;
				}

				var runtimeSettings = EcsactRuntimeSettings.Get();
				testDefaultRuntime = EcsactRuntime.Load(
					runtimeSettings.runtimeLibraryPaths
				);

				SetupMethodsUI(
					ui,
					"async",
					EcsactRuntime.Async.methods,
					testDefaultRuntime.async.availableMethods
				);

				SetupMethodsUI(
					ui,
					"core",
					EcsactRuntime.Core.methods,
					testDefaultRuntime.core.availableMethods
				);

				SetupMethodsUI(
					ui,
					"dynamic",
					EcsactRuntime.Dynamic.methods,
					testDefaultRuntime.dynamic.availableMethods
				);

				SetupMethodsUI(
					ui,
					"meta",
					EcsactRuntime.Meta.methods,
					testDefaultRuntime.meta.availableMethods
				);

				SetupMethodsUI(
					ui,
					"serialize",
					EcsactRuntime.Serialize.methods,
					testDefaultRuntime.serialize.availableMethods
				);

				SetupMethodsUI(
					ui,
					"static",
					EcsactRuntime.Static.methods,
					testDefaultRuntime.@static.availableMethods
				);

				SetupMethodsUI(
					ui,
					"wasm",
					EcsactRuntime.Wasm.methods,
					testDefaultRuntime.wasm.availableMethods
				);

				var runtimeSettingsContainer =
					ui.Q<IMGUIContainer>("runtime-settings-container");

				runtimeSettingsEditor = Editor.CreateEditor(runtimeSettings);

				runtimeSettingsContainer.onGUIHandler = () => {
					runtimeSettingsEditor.OnInspectorGUI();
				};

				var wasmRuntimeSettingsContainer =
					ui.Q<IMGUIContainer>("wasm-runtime-settings-container");
				wasmRuntimeSettingsEditor =
					EcsactWasmEditorInternalUtil.GetEcsactWasmRuntimeSettingsEditor();
				wasmRuntimeSettingsContainer.onGUIHandler = () => {
					wasmRuntimeSettingsEditor.OnInspectorGUI();
				};
			},
			deactivateHandler = () => {
				if(testDefaultRuntime != null) {
					EcsactRuntime.Free(testDefaultRuntime);
					testDefaultRuntime = null;
				}

				if(runtimeSettingsEditor != null) {
					Editor.DestroyImmediate(runtimeSettingsEditor);
					runtimeSettingsEditor = null;
				}
			},
			keywords = new HashSet<string>(new[] {
				"Ecsact",
				"ECS",
				"ECS Plugin",
				"Plugin",
				"Runtime",
				"Library",
			}),
		};
	}
}
