using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Ecsact.Editor {

[CustomEditor(typeof(Ecsact.PreferredEntityGameObject))]
public class PreferredEntityGameObjectEditor : UnityEditor.Editor {
	public override void OnInspectorGUI() {
		EditorGUILayout.HelpBox(
			"This Game Object is part of an Ecsact Game Object Pool and has be " +
				"set as 'preferred' for a specific entity.",
			MessageType.Info
		);
	}
}

} // namespace Ecsact.Editor
