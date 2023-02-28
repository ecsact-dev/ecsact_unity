using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

#nullable enable

namespace Ecsact.Editor {

[InitializeOnLoad]
[CustomEditor(typeof(EcsactRuntimeSettings))]
public class EcsactRuntimeSettingsEditor : UnityEditor.Editor {
	private static bool loadingUnitySyncTypes = false;
	// Only for the sake of displaying whats happening in editor
	private static Assembly? currentCheckingAssembly;
	private static List<global::System.Type>      potentialUnitySyncTypes = new();
	private static Dictionary<string, MonoScript> cachedMonoScriptLookups = new();

	static EcsactRuntimeSettingsEditor() {
		UnityEditor.EditorApplication.delayCall += () => { DelayedStaticInit(); };
	}

	private static void DelayedStaticInit() {
		var settings = EcsactRuntimeSettings.Get();

		EditorCoroutineUtility.StartCoroutineOwnerless(LoadAssemblies(settings));
	}

	public override bool RequiresConstantRepaint() {
		return loadingUnitySyncTypes;
	}

	public void OnEnable() {
		if(potentialUnitySyncTypes.Count == 0) {
			var settings = EcsactRuntimeSettings.Get();
			EditorCoroutineUtility.StartCoroutine(LoadAssemblies(settings), this);
		}
	}

	private static global::System.Collections
		.IEnumerator LoadAssemblies(EcsactRuntimeSettings settings) {
		if(loadingUnitySyncTypes) yield break;

		var progressId = -1;
		var cancelled = false;
		if(settings.enableUnitySync) {
			progressId = Progress.Start(
				"Ecsact Unity Sync Lookup",
				options: Progress.Options.Managed
			);

			Progress.RegisterCancelCallback(progressId, () => {
				cancelled = true;
				Progress.Remove(progressId);
				progressId = -1;
				return true;
			});
		}

		loadingUnitySyncTypes = true;
		var delay = 0.001f;

		try {
			if(!cancelled) {
				yield return new WaitForSecondsRealtime(delay);
			}
			var assemblies = global::System.AppDomain.CurrentDomain.GetAssemblies();
			if(!cancelled) {
				yield return new WaitForSecondsRealtime(delay);
			}
			foreach(var assembly in assemblies) {
				if(cancelled) break;

				currentCheckingAssembly = assembly;
				if(progressId != -1) {
					Progress.SetDescription(progressId, assembly.FullName);
				}
				var types = assembly.GetTypes();
				yield return new WaitForSecondsRealtime(delay);
				int i = 0;
				foreach(var type in types) {
					if(cancelled) break;

					if((i % 100) == 0) {
						yield return new WaitForSecondsRealtime(delay);
					}

					if(UnitySync.UnitySyncMonoBehaviours.HasInterfaces(type)) {
						potentialUnitySyncTypes.Add(type);
						if(settings.enableUnitySync) {
							EnsureNewUnitySyncScriptExists(
								settings.unitySyncScripts,
								type,
								settings
							);
						}
					}

					i += 1;
				}
			}

			if(settings.enableUnitySync) {
				if(EnsureUnitySyncScripts(settings.unitySyncScripts, settings)) {
					EditorUtility.SetDirty(settings);
				}
			}
		} finally {
			currentCheckingAssembly = null;
			loadingUnitySyncTypes = false;
			if(progressId != -1) {
				Progress.Finish(progressId);
			}
		}
	}

	private static bool RemoveUnknownUnitySyncScripts(
		List<EcsactRuntimeSettings.UnitySyncScriptInfo> unitySyncScripts,
		UnityEngine.Object? context = null
	) {
		var removeAmount = unitySyncScripts.RemoveAll(info => {
			var index = potentialUnitySyncTypes.FindIndex(
				type => info.scriptAssemblyQualifiedName == type.AssemblyQualifiedName
			);
			var shouldRemove = index == -1;
			if(shouldRemove) {
				var shortName =
					info.scriptAssemblyQualifiedName.Split(",", count: 2)[0];
				Debug.Log(
					$"<color=grey>Removing old Ecsact Unity Sync script:</color> " +
						$"<color=orange>{shortName}</color>",
					context
				);
			}

			return shouldRemove;
		});

		return removeAmount > 0;
	}

	private static bool EnsureNewUnitySyncScriptExists(
		List<EcsactRuntimeSettings.UnitySyncScriptInfo> unitySyncScripts,
		global::System.Type                             type,
		UnityEngine.Object? context = null
	) {
		var index = unitySyncScripts.FindIndex(
			info => info.scriptAssemblyQualifiedName == type.AssemblyQualifiedName
		);

		if(index == -1) {
			Debug.Log(
				$"<color=grey>Adding new Ecsact Unity Sync script:</color> " +
					$"<color=lightblue>{type.FullName}</color>",
				context
			);

			unitySyncScripts.Add(new EcsactRuntimeSettings.UnitySyncScriptInfo {
				scriptEnabled = true,
				scriptAssemblyQualifiedName = type.AssemblyQualifiedName,
			});

			return true;
		}

		return false;
	}

	private static bool AddNewUnitySyncScripts(
		List<EcsactRuntimeSettings.UnitySyncScriptInfo> unitySyncScripts,
		UnityEngine.Object? context = null
	) {
		var addCount = 0;

		foreach(var type in potentialUnitySyncTypes) {
			if(EnsureNewUnitySyncScriptExists(unitySyncScripts, type, context)) {
				addCount += 1;
			}
		}

		return addCount > 1;
	}

	private static bool EnsureUnitySyncScripts(
		List<EcsactRuntimeSettings.UnitySyncScriptInfo> unitySyncScripts,
		UnityEngine.Object? context = null
	) {
		var removedScripts =
			RemoveUnknownUnitySyncScripts(unitySyncScripts, context);
		var addedScripts = AddNewUnitySyncScripts(unitySyncScripts, context);
		return removedScripts || addedScripts;
	}

	private MonoScript? FindMonoScript(string scriptAssemblyQualifiedName) {
		if(cachedMonoScriptLookups.ContainsKey(scriptAssemblyQualifiedName)) {
			return cachedMonoScriptLookups[scriptAssemblyQualifiedName];
		}

		foreach(var monoScript in Resources.FindObjectsOfTypeAll<MonoScript>()) {
			var type = monoScript.GetClass();
			if(type != null) {
				if(type.AssemblyQualifiedName == scriptAssemblyQualifiedName) {
					cachedMonoScriptLookups[scriptAssemblyQualifiedName] = monoScript;
					return monoScript;
				}
			}
		}

		return null;
	}

	public override void OnInspectorGUI() {
		var settings = (target as EcsactRuntimeSettings)!;

		DrawDefaultInspector();

		var oldEnableUnitySync = settings.enableUnitySync;
		settings.enableUnitySync =
			EditorGUILayout.Toggle("Enable Unity Sync", settings.enableUnitySync);
		if(oldEnableUnitySync != settings.enableUnitySync) {
			if(settings.enableUnitySync) {
				EnsureUnitySyncScripts(settings.unitySyncScripts, settings);
			} else {
				settings.unitySyncScripts = new();
			}

			EditorUtility.SetDirty(settings);
		}

		if(settings.runner == EcsactRuntimeSettings.RunnerType.DefaultRunner) {
			settings.defaultRegistry.registryName =
				EditorGUILayout.TextField("Registry Name", "Default Registry");
			settings.defaultRegistry.updateMethod =
				(EcsactRuntimeDefaultRegistry.UpdateMethod)EditorGUILayout.EnumPopup(
					"Update Method",
					settings.defaultRegistry.updateMethod
				);
		}

		if(settings.runner == EcsactRuntimeSettings.RunnerType.AsyncRunner) {
			settings.tickRate =
				EditorGUILayout.IntField("Tick Rate", settings.tickRate);
		}

		if(settings.enableUnitySync) {
			EditorGUILayout.LabelField(
				label: $"Unity Sync Scripts ({settings.unitySyncScripts.Count})",
				style: EditorStyles.boldLabel
			);
			++EditorGUI.indentLevel;
			DrawUnitySyncScriptsGUI(settings);
			--EditorGUI.indentLevel;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void DrawUnitySyncScriptsGUI(EcsactRuntimeSettings settings) {
		for(int i = 0; settings.unitySyncScripts!.Count > i; ++i) {
			var scriptInfo = settings.unitySyncScripts[i];
			EditorGUILayout.BeginHorizontal();
			var type =
				global::System.Type.GetType(scriptInfo.scriptAssemblyQualifiedName);

			scriptInfo.scriptEnabled =
				EditorGUILayout.ToggleLeft(type.FullName, scriptInfo.scriptEnabled);

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField(
				obj: FindMonoScript(scriptInfo.scriptAssemblyQualifiedName),
				objType: typeof(MonoScript),
				allowSceneObjects: false
			);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();

			settings.unitySyncScripts[i] = scriptInfo;
		}

		if(loadingUnitySyncTypes) {
			if(currentCheckingAssembly != null) {
				EditorGUILayout.LabelField(
					$"Searching assembly {currentCheckingAssembly.FullName}"
				);
			}
		} else if(potentialUnitySyncTypes.Count == 0) {
			EditorGUILayout.HelpBox(
				"No ecsact unity sync scripts found in project. Create a " +
					"MonoBehaviour with one or more of the Ecsact.UnitySync interfaces - " +
					"IRequired<>, IOnInitComponent<>, IOnUpdateComponent<>, or " +
					"IOnRemoveComponent<>.",
				type: MessageType.Warning
			);
		}
	}
}

} // namespace Ecsact.Editor
