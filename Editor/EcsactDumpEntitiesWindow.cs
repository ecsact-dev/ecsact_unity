using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EcsatDumpEntitiesWindow : EditorWindow {
	static string     dumpOutputPath = "";
	static bool       overwriteOutput = false;
	static bool       sceneEntitiesFoldout = true;
	static List<bool> componentFoldouts = new();
	const bool        COMPONENT_FOLDOUT_DEFAULT = false;

	[MenuItem("Window/Ecsact/Dump Entities")]
	static void Init() {
		var window = EditorWindow.GetWindow(typeof(EcsatDumpEntitiesWindow));
		var windowTitle = new GUIContent {};
		windowTitle.text = "Ecsact - Dump Entities";
		window.titleContent = windowTitle;
		window.Show();
	}

	void OnEnable() {
	}

	void OnDisable() {
	}

	private bool ShouldFoldoutEntityAtIndex(int index) {
		if(index >= componentFoldouts.Count) {
			return COMPONENT_FOLDOUT_DEFAULT;
		}

		return componentFoldouts[index];
	}

	private bool SetEntityFoldoutAtIndex(int index, bool foldout) {
		while(index >= componentFoldouts.Count) {
			componentFoldouts.Add(COMPONENT_FOLDOUT_DEFAULT);
		}

		componentFoldouts[index] = foldout;

		return foldout;
	}

	private void DrawEntityGUI(int index, Ecsact.DynamicEntity dynamicEntity) {
		var foldout = ShouldFoldoutEntityAtIndex(index);

		var layoutStyle = new GUIStyle();
		layoutStyle.padding.left = 20;

		GUILayout.BeginHorizontal(new GUIStyle(layoutStyle));
		foldout = SetEntityFoldoutAtIndex(
			index,
			EditorGUILayout.Foldout(foldout, dynamicEntity.name)
		);
		GUILayout.EndHorizontal();

		if(foldout) {
			layoutStyle.padding.left += 20;
			GUILayout.BeginVertical(layoutStyle);
			foreach(var component in dynamicEntity.ecsactComponents) {
				var componentName = component._ecsactComponentNameEditorOnly;
				EditorGUILayout.LabelField(componentName);
			}
			GUILayout.EndVertical();
		}
	}

	void OnGUI() {
		var dynamicEntities = GameObject.FindObjectsOfType<Ecsact.DynamicEntity>();
		if(Application.isPlaying) {
			EditorGUILayout.HelpBox(
				"Entity list is unavailable during play mode",
				MessageType.Info
			);
		} else {
			sceneEntitiesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
				sceneEntitiesFoldout,
				"Scene Entities"
			);
			if(sceneEntitiesFoldout) {
				var index = 0;
				foreach(var dynamicEntity in dynamicEntities) {
					DrawEntityGUI(index, dynamicEntity);
					index += 1;
				}
			}
		}

		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space();

		GUILayout.BeginHorizontal();

		EditorGUILayout.PrefixLabel("Output Path");
		dumpOutputPath = EditorGUILayout.TextField(dumpOutputPath);

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(" ");
		if(!string.IsNullOrEmpty(dumpOutputPath)) {
			var fullPath = Path.GetFullPath(dumpOutputPath);
			EditorGUILayout.LabelField(fullPath);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		overwriteOutput = EditorGUILayout.Toggle("Overwrite", overwriteOutput);

		var invalidSave = string.IsNullOrEmpty(dumpOutputPath) ||
			Directory.Exists(dumpOutputPath) ||
			(File.Exists(dumpOutputPath) && !overwriteOutput);

		EditorGUI.BeginDisabledGroup(invalidSave);
		if(GUILayout.Button("Dump Entities")) {
			SaveEntityDump(dumpOutputPath, dynamicEntities);
		}
		EditorGUI.EndDisabledGroup();
		GUILayout.EndHorizontal();
	}

	void SaveEntityDump(
		string dumpOutputPath,
		Ecsact.DynamicEntity[] dynamicEntities
	) {
		var dumpOutputFile = File.Create(dumpOutputPath);

		if(Application.isPlaying) {
			var registry = Ecsact.Defaults.Registry.ID;

			try {
				Ecsact.Defaults.Runtime.serialize.DumpEntities(registry, bytes => {
					dumpOutputFile.Write(bytes);
				});
			} finally {
				dumpOutputFile.Dispose();
			}

		} else {
			CreateTempReg(dumpOutputFile, dynamicEntities);
		}
	}

	void CreateTempReg(
		FileStream dumpOutputFile,
		Ecsact.DynamicEntity[] dynamicEntities
	) {
		var settings = EcsactRuntimeSettings.Get();
		var tempRuntime =
			EcsactRuntime.Load(settings.GetValidRuntimeLibraryPaths());

		try {
			var registry = tempRuntime.core.CreateRegistry("TempDumpEntities");

			foreach(var dynamicEntity in dynamicEntities) {
				var entityId = tempRuntime.core.CreateEntity(registry);

				foreach(var ecsactComponent in dynamicEntity.ecsactComponents) {
					if(ecsactComponent.data != null) {
						tempRuntime.core.AddComponent(
							registry,
							entityId,
							ecsactComponent.id,
							ecsactComponent.data
						);
					} else {
						Debug.LogWarning($"No Data for component {ecsactComponent.id}");
					}
				}
			}

			tempRuntime.serialize.DumpEntities(registry, bytes => {
				dumpOutputFile.Write(bytes);
			});
		} finally {
			EcsactRuntime.Free(tempRuntime);
			dumpOutputFile.Dispose();
		}
	}
}
