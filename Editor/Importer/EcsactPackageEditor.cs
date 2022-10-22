using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

#nullable enable

[CustomEditor(typeof(EcsactPackage))]
[CanEditMultipleObjects]
public class EcsactPackageEditor : Editor {
	private static Dictionary<string, bool> pkgFoldouts = new();
	private static bool                     componentsFoldout = true;

	public override void OnInspectorGUI() {
		List<EcsactPackage> pkgList = new List<EcsactPackage>();

		foreach(var target in targets) {
			var pkg = (EcsactPackage)target;
			pkgList.Add(pkg);
		}

		foreach(var pkg in pkgList) {
			if(!pkgFoldouts.ContainsKey(pkg.name)) {
				pkgFoldouts.Add(pkg.name, false);
			}

			pkgFoldouts[pkg.name] = EditorGUILayout.BeginFoldoutHeaderGroup(
				pkgFoldouts[pkg.name],
				pkg.name
			);
			EditorGUI.BeginDisabledGroup(true);

			EditorGUILayout.LabelField(
				"Imports",
				pkg.imports.Count == 0 ? "(none)" : pkg.imports[0]
			);

			for(int i = 1; pkg.imports.Count > i; ++i) {
				EditorGUILayout.LabelField(" ", pkg.imports[i]);
			}

			componentsFoldout = EditorGUILayout.Foldout(
				componentsFoldout,
				$"Components ({pkg.components.Count})"
			);
			if(componentsFoldout) {
				EditorGUI.indentLevel += 1;
				foreach(var comp in pkg.components) {
					EditorGUILayout.LabelField(comp.full_name);
					EditorGUI.indentLevel += 1;
					foreach(var field in comp.fields) {
						var fieldName = field.field_name;
						if(field.field_type.length > 1) {
							fieldName += $"[{field.field_type.length}]";
						}
						EditorGUILayout.LabelField(fieldName, field.field_type.type);
					}
					EditorGUI.indentLevel -= 1;
				}
				EditorGUI.indentLevel -= 1;
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndFoldoutHeaderGroup();
		}
	}
}
