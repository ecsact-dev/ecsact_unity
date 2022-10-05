using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

#nullable enable

// https://docs.unity3d.com/Manual/AssemblyDefinitionFileFormat.html
[System.Serializable]
struct UnityAssemblyDefinitionFile {
	public bool allowUnsafeCode;
	public bool autoReferenced;
	public bool noEngineReferences;
	public List<string> defineConstraints;
	public List<string> includePlatforms;
	public List<string> excludePlatforms;
	public string name;
	public List<string> optionalUnityReferences;
	public bool overrideReferences;
	public List<string> precompiledReferences;
	public List<string> references;
}

namespace Ecsact.Editor {

[CustomEditor(typeof(CsharpSystemImplSettings))]
public class CsharpSystemImplSettingsEditor : UnityEditor.Editor {
	private static List<global::System.Type>? systemLikeTypes;

	public override void OnInspectorGUI() {
		var settings = (target as CsharpSystemImplSettings)!;

		settings.systemImplsAssembly = EditorGUILayout.ObjectField(
			obj: settings.systemImplsAssembly,
			objType: typeof(UnityEditorInternal.AssemblyDefinitionAsset),
			allowSceneObjects: false,
			label: "Assembly"
		) as UnityEditorInternal.AssemblyDefinitionAsset;

		if(settings.systemImplsAssembly != null) {
			var asmDefJson = global::System.Text.Encoding.Default.GetString(
				settings.systemImplsAssembly.bytes
			);
			var asmDef = JsonUtility.FromJson<UnityAssemblyDefinitionFile>(
				asmDefJson
			);

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

				foreach(var systemLikeType in systemLikeTypes) {
					var systemLikeId = Ecsact.Util.GetSystemID(systemLikeType);
					var methods = implDict.GetValueOrDefault(systemLikeId, new());
					DrawSystemImplDetail(systemLikeId, systemLikeType, methods);
				}

			} else {
				EditorGUILayout.HelpBox(
					$"Unable to load assembly definition by name: {asmDef.name}",
					MessageType.Error
				);
			}
		}
	}

	private string GetMethodFullName
		( MethodInfo methodInfo
		)
	{
		return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
	}

	private void DrawSystemImplDetail
		( global::System.Int32  systemLikeId
		, global::System.Type   systemLikeType
		, List<MethodInfo>      methods
		)
	{
		string implName = "";
		if(methods.Count == 0) {
			implName = "(none)";
		} else if(methods.Count == 1) {
			implName = GetMethodFullName(methods[0]);
		} else {
			implName = "(multiple methods)";
		}
		EditorGUILayout.LabelField(systemLikeType.FullName, implName);
	}
}

}
