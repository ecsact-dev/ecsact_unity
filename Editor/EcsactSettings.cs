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
	public const string        assetPath = "Assets/Editor/EcsactSettings.asset";
	public const string        path = "Project/Ecsact";
	public const SettingsScope scope = SettingsScope.Project;

	public bool   runtimeBuilderEnabled = true;
	public string runtimeBuilderOutputPath = "Assets/Plugins/EcsactRuntime";
	public string runtimeBuilderTempDirectory = "";
	public bool   runtimeBuilderDebugBuild = false;
	public bool   runtimeBuilderPrintSubcommandStdout = false;
	public bool   runtimeBuilderPrintSubcommandStderr = false;

	public string runtimeBuilderCompilerPath = "";

	static EcsactSettings() {
		EcsactRuntimeSettings.editorValidateEvent += OnRuntimeSettingsValidate;
	}

	public static EcsactSettings GetOrCreateSettings() {
		var settings = AssetDatabase.LoadAssetAtPath<EcsactSettings>(assetPath);
		if(settings == null) {
			if(!System.IO.Directory.Exists("Assets/Editor")) {
				System.IO.Directory.CreateDirectory("Assets/Editor");
			}
			settings = ScriptableObject.CreateInstance<EcsactSettings>();
			AssetDatabase.CreateAsset(settings, assetPath);
			AssetDatabase.SaveAssetIfDirty(settings);
		}

		return settings;
	}

	internal static SerializedObject GetSerializedSettings() {
		return new SerializedObject(GetOrCreateSettings());
	}

#if UNITY_EDITOR
	static void OnRuntimeSettingsValidate(EcsactRuntimeSettings rtSettings) {
		var settings = GetOrCreateSettings();
		var outputPath = settings.runtimeBuilderOutputPath;
		if(rtSettings.runtimeLibraryPaths.Count == 0) {
			rtSettings.runtimeLibraryPaths.Add("");
		}

		if(settings.runtimeBuilderEnabled) {
			rtSettings.runtimeLibraryPaths[0] = settings.runtimeBuilderOutputPath;
		} else {
			rtSettings.runtimeLibraryPaths[0] = "";
		}
	}

	void OnValidate() {
		var rtSettings = EcsactRuntimeSettings.Get();
		if(rtSettings.runtimeLibraryPaths.Count == 0) {
			rtSettings.runtimeLibraryPaths.Add("");
		}

		if(runtimeBuilderEnabled) {
			rtSettings.runtimeLibraryPaths[0] = runtimeBuilderOutputPath;
		} else {
			rtSettings.runtimeLibraryPaths[0] = "";
		}
	}
#endif
}

[System.Serializable]
class EcsactMethodUIBindings : ScriptableObject {
	public string methodName = "";
	public bool   methodLoaded = false;
}

class EcsactSettingsSettingsProvider : SettingsProvider {
	private static bool showMissingMethods;

	Editor? runtimeSettingsEditor = null;
	Editor? wasmRuntimeSettingsEditor = null;
	Editor? csharpSystemImplSettingsEditor = null;
	IMGUIContainer? runtimeSettingsContainer = null;
	IMGUIContainer? csharpSystemImplSettingsContainer = null;
	DropdownField? sysImplSrcDropdown = null;

	public EcsactSettingsSettingsProvider()
		: base(
				path: EcsactSettings.path,
				scopes: EcsactSettings.scope,
				keywords: new HashSet<string>(new[] {
					"Ecsact",
					"ECS",
					"ECS Plugin",
					"Plugin",
					"Runtime",
					"Library",
				})
			) {
	}

	internal static void SetupMethodsUI(
		TemplateContainer   ui,
		string              moduleName,
		IEnumerable<string> methods,
		IEnumerable<string> availableMethods
	) {
		var coreMethodTemplate =
			ui.Q<TemplateContainer>($"{moduleName}-method-template");

		var parent = coreMethodTemplate.contentContainer.parent;

		var visibleMethods = 0;
		foreach(var method in methods) {
			TemplateContainer clone;

			clone = parent.Q<TemplateContainer>(method);
			if(clone == null) {
				clone = coreMethodTemplate.templateSource.CloneTree();
				clone.name = method;
			}

			var bindings = ScriptableObject.CreateInstance<EcsactMethodUIBindings>();
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

			if(bindings.methodLoaded || showMissingMethods) {
				clone.style.display = DisplayStyle.Flex;
				visibleMethods += 1;
			} else {
				clone.style.display = DisplayStyle.None;
			}

			parent.Add(clone);
		}

		if(visibleMethods > 0) {
			parent.parent.style.display = DisplayStyle.Flex;
		} else {
			parent.parent.style.display = DisplayStyle.None;
		}

		coreMethodTemplate.contentContainer.style.display = DisplayStyle.None;
	}

	private static void DrawMethodsUI(
		TemplateContainer     ui,
		EcsactRuntimeSettings settings
	) {
		var testDefaultRuntime =
			EcsactRuntime.Load(settings.GetValidRuntimeLibraryPaths());
		try {
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
		} finally {
			EcsactRuntime.Free(testDefaultRuntime);
			testDefaultRuntime = null;
		}
	}

	private static void TryDrawMethodsUI(
		TemplateContainer     ui,
		EcsactRuntimeSettings settings
	) {
		var runtimeModulesGroup = ui.Q<GroupBox>("RuntimeModulesGroup");
		try {
			DrawMethodsUI(ui, settings);
			runtimeModulesGroup.style.display = DisplayStyle.Flex;
		} catch(System.Exception err) {
			Debug.LogException(err, settings);
			runtimeModulesGroup.style.display = DisplayStyle.None;
		}
	}

	Ecsact.SystemImplSource SysImplSrcDropdownValue() {
		return (Ecsact.SystemImplSource)sysImplSrcDropdown!.index;
	}

	public override void OnInspectorUpdate() {
		if(runtimeSettingsEditor != null) {
			if(runtimeSettingsEditor.RequiresConstantRepaint()) {
				if(runtimeSettingsContainer != null) {
					runtimeSettingsContainer.MarkDirtyRepaint();
				}
			}
		}

		if(csharpSystemImplSettingsEditor != null) {
			if(csharpSystemImplSettingsEditor.RequiresConstantRepaint()) {
				if(csharpSystemImplSettingsContainer != null) {
					csharpSystemImplSettingsContainer.MarkDirtyRepaint();
				}
			}
		}
	}

	public override void OnActivate(
		string        searchContext,
		VisualElement rootElement
	) {
		var settings = EcsactSettings.GetSerializedSettings();
		var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
			"Packages/dev.ecsact.unity/Editor/EcsactSettings.uxml"
		);
		var ui = template.Instantiate();
		BindingExtensions.Bind(ui, settings);
		rootElement.Add(ui);

		var builderSettingsElement =
			ui.Q<TemplateContainer>("EcsactRuntimeBuilderSettings");
		var rtbEnableToggle = ui.Q<Toggle>("EnableRTB");

		rtbEnableToggle.RegisterValueChangedCallback(evt => {
			if(evt.newValue) {
				builderSettingsElement.style.display = DisplayStyle.Flex;
			} else {
				builderSettingsElement.style.display = DisplayStyle.None;
			}
		});

		var runtimeSettings = EcsactRuntimeSettings.Get();
		TryDrawMethodsUI(ui, runtimeSettings);

		var showMissingMethodsToggle = ui.Q<Toggle>("ShowMissingMethodsToggle");
		showMissingMethodsToggle.RegisterValueChangedCallback(ev => {
			showMissingMethods = ev.newValue;
			TryDrawMethodsUI(ui, runtimeSettings);
		});

		runtimeSettingsContainer =
			ui.Q<IMGUIContainer>("runtime-settings-container");

		runtimeSettingsEditor = Editor.CreateEditor(runtimeSettings);

		runtimeSettingsContainer.onGUIHandler =
			() => { runtimeSettingsEditor.OnInspectorGUI(); };

		sysImplSrcDropdown = ui.Q<DropdownField>("system-impls-source-dropdown");
		sysImplSrcDropdown.index = (int)runtimeSettings.systemImplSource;

		sysImplSrcDropdown.RegisterValueChangedCallback(ev => {
			runtimeSettings.systemImplSource = SysImplSrcDropdownValue();
			EditorUtility.SetDirty(runtimeSettings);
		});

		var wasmRuntimeSettingsContainer =
			ui.Q<IMGUIContainer>("wasm-runtime-settings-container");
		wasmRuntimeSettingsEditor =
			EcsactWasmEditorInternalUtil.GetEcsactWasmRuntimeSettingsEditor();
		wasmRuntimeSettingsContainer.onGUIHandler = () => {
			if(SysImplSrcDropdownValue() == Ecsact.SystemImplSource.WebAssembly) {
				wasmRuntimeSettingsEditor.OnInspectorGUI();
			}
		};

		var csharpSystemImplSettings = Ecsact.Editor.CsharpSystemImplSettings.Get();
		csharpSystemImplSettingsContainer =
			ui.Q<IMGUIContainer>("csharp-system-impl-settings-container");
		csharpSystemImplSettingsEditor =
			Editor.CreateEditor(csharpSystemImplSettings);
		csharpSystemImplSettingsContainer.onGUIHandler = () => {
			if(SysImplSrcDropdownValue() == Ecsact.SystemImplSource.Csharp) {
				csharpSystemImplSettingsEditor.OnInspectorGUI();
			}
		};
	}

	public override void OnDeactivate() {
		if(runtimeSettingsEditor != null) {
			Editor.DestroyImmediate(runtimeSettingsEditor);
			runtimeSettingsEditor = null;
		}
	}
}

static class EcsactSettingsUIElementsRegister {
	[SettingsProvider]
	public static SettingsProvider CreateEcsactSettingsProvider() {
		return new EcsactSettingsSettingsProvider();
	}
}
