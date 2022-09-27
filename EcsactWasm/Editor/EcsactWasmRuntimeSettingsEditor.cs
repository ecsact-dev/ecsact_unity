using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Ecsact.Editor.Internal;

#nullable enable

#if HAS_UNITY_WASM_PACKAGE

[InitializeOnLoad]
[CustomEditor(typeof(EcsactWasmRuntimeSettings))]
public class EcsactWasmRuntimeSettingsEditor : Editor {

	static EcsactWasmRuntimeSettingsEditor() {
		EcsactWasmEditorInternalUtil.GetEcsactWasmRuntimeSettingsEditor = () => {
			var wasmRuntimeSettings = EcsactWasmRuntimeSettings.Get();
			return Editor.CreateEditor(wasmRuntimeSettings);
		};
	}

	static IEnumerable<(WasmAsset, string)> FindWasmAssets() {
		var guids = AssetDatabase.FindAssets($"t:{typeof(WasmAsset)}");
		foreach (var t in guids) {
			var assetPath = AssetDatabase.GUIDToAssetPath(t);
			var asset = AssetDatabase.LoadAssetAtPath<WasmAsset>(assetPath);
			if (asset != null) {
				yield return (asset, assetPath);
			}
		}
	}

	private static void FindSystemImplsInfoLoaded
		( Dictionary<string, WasmInfo>              wasmInfos
		, System.Action<EcsactWasmRuntimeSettings>  doneCallback
		)
	{
		var settings = EcsactWasmRuntimeSettings.Get();
		List<EcsactWasmRuntimeSettings.SystemMapEntry> newEntries = new();
		var systemLikeTypes = Ecsact.Util.GetAllSystemLikeTypes();
		foreach(var (path, wasmInfo) in wasmInfos) {
			var wasmAsset = AssetDatabase.LoadAssetAtPath<WasmAsset>(path);

			foreach(var systemLikeType in systemLikeTypes) {
				var systemLikeName = systemLikeType.FullName;
				var systemImplName = systemLikeName.Replace(".", "__");
				foreach(var exportInfo in wasmInfo.exports) {
					if(IsValidSystemImplExport(exportInfo)) {
						if(exportInfo.name == systemImplName) {
							var entry = new EcsactWasmRuntimeSettings.SystemMapEntry();
							entry.systemId = Ecsact.Util.GetSystemID(systemLikeType);
							entry.wasmAsset = wasmAsset;
							entry.wasmExportName = systemImplName;
							newEntries.Add(entry);
						}
					}
				}
			}
		}

		settings.wasmSystemEntries = newEntries;
		doneCallback(settings);
	}

	public static void FindSystemImpls
		( System.Action<EcsactWasmRuntimeSettings> doneCallback
		)
	{
		var wasmInfoDict = new Dictionary<string, WasmInfo>();
		var wasmAssets = FindWasmAssets();
		var validWasmInfosCount = wasmAssets.Count();
		
		foreach(var (wasmAsset, path) in wasmAssets) {
			WasmInfo.Load(
				wasmAssetPath: path,
				successCallback: wasmInfo => {
					wasmInfoDict.Add(path, wasmInfo);
					if(wasmInfoDict.Count == validWasmInfosCount) {
						EditorApplication.delayCall += () => {
							FindSystemImplsInfoLoaded(wasmInfoDict, doneCallback);
						};
					}
				},
				errorCallback: () => {
					validWasmInfosCount -= 1;
					if(wasmInfoDict.Count == validWasmInfosCount) {
						EditorApplication.delayCall += () => {
							FindSystemImplsInfoLoaded(wasmInfoDict, doneCallback);
						};
					}
				}
			);
		}
	}

	private List<Type>? systemLikeTypes;
	private Dictionary<Int32, WasmInfo?> wasmInfos = new();
	private bool allWasmInfoLoaded = false;

	SerializedProperty? useDefaultLoader;
	SerializedProperty? autoFindSystemImpls;

	void OnEnable() {
		useDefaultLoader = serializedObject.FindProperty("useDefaultLoader");
		autoFindSystemImpls =
			serializedObject.FindProperty("autoFindSystemImpls");
	}

	static bool IsValidSystemImplExport
		( WasmInfo.ExternInfo exportInfo
		)
	{
		return
			exportInfo.type == "WASM_EXTERN_FUNC" &&
			exportInfo.results!.Count == 0 &&
			exportInfo.@params!.Count == 1;
	}

	public override void OnInspectorGUI() {
		var settings = target as EcsactWasmRuntimeSettings;
		if(settings == null) return;

		EditorGUI.BeginChangeCheck();
		
		if(systemLikeTypes == null) {
			systemLikeTypes = Ecsact.Util.GetAllSystemLikeTypes().ToList();
		}

		EditorGUILayout.PropertyField(useDefaultLoader);
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(autoFindSystemImpls);
		if(EditorGUI.EndChangeCheck()) {
			if(autoFindSystemImpls.boolValue) {
				FindSystemImpls(OnSystemImplsAutoChange);
			}
		}

		EditorGUILayout.LabelField(
			label: "System Implementations",
			style: EditorStyles.boldLabel
		);

		allWasmInfoLoaded = true;

		EditorGUI.BeginDisabledGroup(settings.autoFindSystemImpls);

		foreach(Type systemLikeType in systemLikeTypes) {
			EditorGUILayout.BeginHorizontal();

			var systemLikeId = Ecsact.Util.GetSystemID(systemLikeType);

			EditorGUILayout.LabelField(
				"    " + systemLikeType.FullName,
				new GUILayoutOption[]{
					GUILayout.ExpandWidth(true)
				}
			);
			var entry = settings.wasmSystemEntries.Find(
				entry => entry.systemId == systemLikeId
			);
			if(entry == null) {
				settings.wasmSystemEntries.Add(new());
				entry = settings.wasmSystemEntries.Last();
			}
			entry.systemId = systemLikeId;
			entry.wasmAsset = EditorGUILayout.ObjectField(
				obj: entry.wasmAsset,
				objType: typeof(WasmAsset),
				allowSceneObjects: false,
				options: new GUILayoutOption[]{GUILayout.Width(200)}
			) as WasmAsset;
			
			if(entry.wasmAsset != null) {
				if(!wasmInfos.ContainsKey(systemLikeId)) {
					wasmInfos.Add(systemLikeId, null);
					WasmInfo.Load(
						AssetDatabase.GetAssetPath(entry.wasmAsset),
						loadedWasmInfo => { wasmInfos[systemLikeId] = loadedWasmInfo; },
						() => { wasmInfos.Remove(systemLikeId); },
						settings
					);
				}
			}

			var wasmInfo = wasmInfos.ContainsKey(systemLikeId)
				? wasmInfos[systemLikeId]
				: null;

			if(wasmInfo == null) {
				allWasmInfoLoaded = false;
			}

			EditorGUI.BeginDisabledGroup(entry.wasmAsset == null);
			var dropdownContent = new GUIContent(entry.wasmExportName);
			var dropdownPressed = EditorGUILayout.DropdownButton(
				dropdownContent,
				FocusType.Keyboard,
				options: new GUILayoutOption[]{GUILayout.Width(200)}
			);
			EditorGUI.EndDisabledGroup();

			if(dropdownPressed && wasmInfo != null) {
				var dropdownMenu = new GenericMenu();
				dropdownMenu.allowDuplicateNames = false;
				foreach(var export in wasmInfo.exports) {
					var menuItemContent = new GUIContent(export.name);

					if(IsValidSystemImplExport(export)) {
						dropdownMenu.AddItem(
							menuItemContent,
							entry.wasmExportName == export.name,
							() => { entry.wasmExportName = export.name; }
						);
					} else {
						dropdownMenu.AddDisabledItem(menuItemContent, false);
					}
				}
				dropdownMenu.ShowAsContext();
			}

			EditorGUILayout.EndHorizontal();
		}

		EditorGUI.EndDisabledGroup();

		if (EditorGUI.EndChangeCheck()) {
			EditorUtility.SetDirty(settings);
		}

		serializedObject.ApplyModifiedProperties();
	}

	void OnSystemImplsAutoChange
		( EcsactWasmRuntimeSettings settings
		)
	{
		serializedObject.ApplyModifiedProperties();
	}

	public override bool RequiresConstantRepaint() {
		return !allWasmInfoLoaded;
	}
}

#else

[InitializeOnLoad]
public class EcsactWasmRuntimeSettingsEditor : Editor {
	static EcsactWasmRuntimeSettingsEditor() {
		EcsactWasmEditorInternalUtil.GetEcsactWasmRuntimeSettingsEditor = () => {
			return ScriptableObject.CreateInstance<EcsactWasmRuntimeSettingsEditor>();
		};
	}

	private static UnityEditor.PackageManager.Requests.AddRequest? addRequest;

	public override void OnInspectorGUI() {
		var packageUrl = "https://github.com/seaube/unity-wasm.git";
		EditorGUILayout.Space();
		EditorGUILayout.HelpBox(
			"The Unity Wasm package must be installed to use Wasm with Ecsact. " +
			$"It can be found here on github {packageUrl} or you can add it by " +
			"clicking the button below.",
			MessageType.Info,
			true
		);

		EditorGUI.BeginDisabledGroup(addRequest != null);
		if(addRequest != null && addRequest.IsCompleted) {
			addRequest = null;
		}

		if(EditorGUILayout.LinkButton("Add 'Unity Wasm' Package")) {
			addRequest = UnityEditor.PackageManager.Client.Add(packageUrl);
		}

		EditorGUI.EndDisabledGroup();
	}
}

#endif // HAS_UNITY_WASM_PACKAGE
