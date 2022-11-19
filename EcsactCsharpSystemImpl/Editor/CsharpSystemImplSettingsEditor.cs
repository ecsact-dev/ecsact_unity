using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using EcsactInternal;

#nullable enable

// https://docs.unity3d.com/Manual/AssemblyDefinitionFileFormat.html
[System.Serializable]
struct UnityAssemblyDefinitionFile {
	public bool         allowUnsafeCode;
	public bool         autoReferenced;
	public bool         noEngineReferences;
	public List<string> defineConstraints;
	public List<string> includePlatforms;
	public List<string> excludePlatforms;
	public string       name;
	public List<string> optionalUnityReferences;
	public bool         overrideReferences;
	public List<string> precompiledReferences;
	public List<string> references;
}

namespace Ecsact.Editor {

[InitializeOnLoad]
[CustomEditor(typeof(CsharpSystemImplSettings))]
public class CsharpSystemImplSettingsEditor : UnityEditor.Editor {
	const float maxWidthMethodDetails = 400f;
	const float minWidthMethodDetails = 300f;
	private static GUILayoutOption[] methodDetailsLayoutOptions = new[] {
		GUILayout.MaxWidth(maxWidthMethodDetails),
		GUILayout.MinWidth(minWidthMethodDetails),
	};
	private static List<global::System.Type>? systemLikeTypes;

	static CsharpSystemImplSettingsEditor() {
		EditorApplication.delayCall += () => { InitializeDelayed(); };
	}

	private static void InitializeDelayed() {
		var settings = CsharpSystemImplSettings.Get();
		var runtimeSettings = EcsactRuntimeSettings.Get();
		EnsureDefaultCsharpSystemImplsAssemblyName(settings, runtimeSettings);
	}

	private static void EnsureDefaultCsharpSystemImplsAssemblyName(
		CsharpSystemImplSettings settings,
		EcsactRuntimeSettings    runtimeSettings
	) {
		var newDefault = "";
		if(settings.systemImplsAssembly != null) {
			var asmDefJson = global::System.Text.Encoding.Default.GetString(
				settings.systemImplsAssembly.bytes
			);
			var asmDef =
				JsonUtility.FromJson<UnityAssemblyDefinitionFile>(asmDefJson);

			newDefault = asmDef.name;
		} else {
			newDefault = "";
		}

		if(runtimeSettings.defaultCsharpSystemImplsAssemblyName != newDefault) {
			runtimeSettings.defaultCsharpSystemImplsAssemblyName = newDefault;
			EditorUtility.SetDirty(runtimeSettings);
		}
	}

	public override void OnInspectorGUI() {
		var settings = (target as CsharpSystemImplSettings)!;

		EditorGUI.BeginChangeCheck();
		settings.systemImplsAssembly =
			EditorGUILayout
				.ObjectField(
					obj: settings.systemImplsAssembly,
					objType: typeof(UnityEditorInternal.AssemblyDefinitionAsset),
					allowSceneObjects: false,
					label: "Assembly"
				) as UnityEditorInternal.AssemblyDefinitionAsset;

		if(EditorGUI.EndChangeCheck()) {
			EnsureDefaultCsharpSystemImplsAssemblyName(
				settings,
				EcsactRuntimeSettings.Get()
			);
			EditorUtility.SetDirty(settings);
			AssetDatabase.SaveAssetIfDirty(settings);
		}

		if(settings.systemImplsAssembly != null) {
			var asmDefJson = global::System.Text.Encoding.Default.GetString(
				settings.systemImplsAssembly.bytes
			);
			var asmDef =
				JsonUtility.FromJson<UnityAssemblyDefinitionFile>(asmDefJson);

			if(!asmDef.noEngineReferences) {
				EditorGUILayout.HelpBox(
					$"The assembly definition {asmDef.name} has engine references " +
						"enabled. Ecsact system implementations run on multiple threads " +
						"and should not use engine apis that are not thread safe.",
					MessageType.Warning,
					wide: false
				);
			}

			var assembly = Assembly.Load(asmDef.name);
			if(assembly != null) {
				var implDict = new Dictionary<int, List<MethodInfo>>();
				foreach(var type in assembly.GetTypes()) {
					foreach(var method in type.GetMethods()) {
						var defaultSystemImplAttr =
							method.GetCustomAttribute<Ecsact.DefaultSystemImplAttribute>();
						if(defaultSystemImplAttr == null) continue;

						var systemLikeId = defaultSystemImplAttr.systemLikeId;
						if(!implDict.ContainsKey(systemLikeId)) {
							implDict.Add(systemLikeId, new());
						}

						implDict[systemLikeId].Add(method);
					}
				}

				if(systemLikeTypes == null) {
					systemLikeTypes = Ecsact.Util.GetAllSystemLikeTypes().ToList();
				}

				if(implDict.Count == 0) {
					EditorGUILayout.HelpBox(
						$"No system implementations detected in assembly {asmDef.name}. " +
							"Add the Ecsact.DefaultSystemImpl attribute to a public static " +
							$"method found in the {asmDef.name} assembly.",
						MessageType.Info,
						wide: false
					);
				}

				foreach(var systemLikeType in systemLikeTypes) {
					var systemLikeId = Ecsact.Util.GetSystemID(systemLikeType);
					var methods = implDict.GetValueOrDefault(systemLikeId, new());
					DrawSystemImplDetail(systemLikeId, systemLikeType, methods);
				}

			} else {
				EditorGUILayout.HelpBox(
					$"Unable to load assembly definition by name: {asmDef.name}",
					MessageType.Error,
					wide: false
				);
			}
		} else {
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");
			var pressed = GUILayout.Button(
				"Create New Assembly Definition",
				new[] { GUILayout.MaxWidth(240) }
			);
			GUILayout.EndHorizontal();
			if(pressed) {
				var newAsmDefPath = EditorUtility.SaveFilePanelInProject(
					title: "Ecsact System Implementations Assembly Definition",
					defaultName: "SystemImpls.asmdef",
					extension: "asmdef",
					message: "Choose a location where your C# system implementations " +
						"definition is stored. Make sure to put it in a separate folder."
				);

				if(!string.IsNullOrEmpty(newAsmDefPath)) {
					var newAsmDef = new UnityAssemblyDefinitionFile();
					newAsmDef.name = $"{Application.identifier}.EcsactSystemImpls";
					newAsmDef.autoReferenced = true;
					newAsmDef.noEngineReferences = false;
					newAsmDef.references = new();
					newAsmDef.references.Add("GUID:2d10fa57d8150f7499b7579b289a41a2");
					newAsmDef.references.Add("GUID:6e7029456009ab94da19a8f93d5e3523");
					global::System.IO.File.WriteAllText(
						path: newAsmDefPath,
						contents: JsonUtility.ToJson(newAsmDef, true)
					);
					AssetDatabase.ImportAsset(
						newAsmDefPath,
						ImportAssetOptions.ForceSynchronousImport
					);
					settings.systemImplsAssembly =
						AssetDatabase
							.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(
								newAsmDefPath
							);
					EditorUtility.SetDirty(settings);
					AssetDatabase.SaveAssetIfDirty(settings);
				}
			}
		}
	}

	private string GetMethodFullName(MethodInfo methodInfo) {
		return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
	}

	private void DrawSystemImplMethodDetails(MethodInfo methodInfo) {
		EditorGUILayout.BeginHorizontal(methodDetailsLayoutOptions);

		var style = new GUIStyle(EditorStyles.label);
		style.richText = true;
		var label = GetMethodFullName(methodInfo);
		var errors =
			DefaultCsharpSystemImplsLoader.ValidateImplMethodInfo(methodInfo);

		if(errors.Count > 0) {
			label = $"<color=red>{label}</color>";
		}

		EditorGUILayout.LabelField(
			label: new GUIContent(label, string.Join("\n", errors)),
			options: new GUILayoutOption[] {},
			style: style
		);

		EditorGUILayout.EndHorizontal();
	}

	private void DrawSystemImplDetail(
		global::System.Int32 systemLikeId,
		global::System.Type  systemLikeType,
		List<MethodInfo>     methods
	) {
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(
			systemLikeType.FullName,
			new GUILayoutOption[] { GUILayout.ExpandWidth(true) }
		);

		if(methods.Count == 0) {
			EditorGUILayout.LabelField(
				label: "(none)",
				options: methodDetailsLayoutOptions
			);
			GUILayout.Space(10f);
		} else if(methods.Count == 1) {
			DrawSystemImplMethodDetails(methods[0]);
		} else {
			EditorGUILayout.BeginVertical(methodDetailsLayoutOptions);
			foreach(var method in methods) {
				DrawSystemImplMethodDetails(method);
			}
			EditorGUILayout.HelpBox(
				"Multiple default system impls for the same system is not allowed.",
				MessageType.Error,
				wide: true
			);
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();
	}
}

}
