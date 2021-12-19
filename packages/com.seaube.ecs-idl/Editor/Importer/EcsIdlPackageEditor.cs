using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
 
#nullable enable

[CustomEditor(typeof(EcsIdlPackage))]
[CanEditMultipleObjects]
public class EcsIdlPackageEditor : Editor {
	private static Dictionary<string, bool> pkgFoldouts =
		new Dictionary<string, bool>();

	public override void OnInspectorGUI() {
		EcsIdlPackage? mainPkg = null;
		List<EcsIdlPackage> pkgList = new List<EcsIdlPackage>();

		foreach(var target in targets) {
			var pkg = (EcsIdlPackage)target;
			pkgList.Add(pkg);

			if(pkg.main) {
				mainPkg = pkg;
			}
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

			EditorGUILayout.Toggle("Main Package", pkg.main);
			EditorGUILayout.LabelField(
				"Imports",
				pkg.imports.Count == 0 ? "(none)" : pkg.imports[0]
			);

			for(int i=1; pkg.imports.Count > i; ++i) {
				EditorGUILayout.LabelField(" ", pkg.imports[i]);
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndFoldoutHeaderGroup();
		}
	}
}
