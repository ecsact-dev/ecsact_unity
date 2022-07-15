using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

#nullable enable

[CustomEditor(typeof(EcsactWasmRuntimeSettings))]
public class EcsactWasmRuntimeSettingsEditor : Editor {

	private List<Type>? systemLikeTypes;
	private Dictionary<Int32, WasmInfo?> wasmInfos = new();
	private bool allWasmInfoLoaded = false;

	public override void OnInspectorGUI() {
		var settings = target as EcsactWasmRuntimeSettings;
		if(settings == null) return;
		
		if(systemLikeTypes == null) {
			systemLikeTypes = Ecsact.Util.GetAllSystemLikeTypes().ToList();
		}

		EditorGUILayout.LabelField(
			label: "System Implementations",
			style: EditorStyles.boldLabel
		);

		allWasmInfoLoaded = true;

		foreach(Type systemLikeType in systemLikeTypes) {
			EditorGUILayout.BeginHorizontal();

			var systemLikeId = Ecsact.Util.GetSystemID(systemLikeType);

			EditorGUILayout.LabelField(systemLikeType.FullName);
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
				allowSceneObjects: false
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
				FocusType.Keyboard
			);
			EditorGUI.EndDisabledGroup();

			if(dropdownPressed && wasmInfo != null) {
				var dropdownMenu = new GenericMenu();
				dropdownMenu.allowDuplicateNames = false;
				foreach(var export in wasmInfo.exports) {
					bool isValidSystemImpl =
						export.type == "WASM_EXTERN_FUNC" &&
						export.results!.Count == 0 &&
						export.@params!.Count == 1;

					var menuItemContent = new GUIContent(export.name);

					if(isValidSystemImpl) {
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

		serializedObject.ApplyModifiedProperties();
	}

	public override bool RequiresConstantRepaint() {
		return !allWasmInfoLoaded;
	}
}
