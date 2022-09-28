using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Ecsact.Editor {
	[CustomEditor(typeof(Ecsact.DynamicEntity))]
	public class DynamicEntityEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			if(Application.isPlaying) {
				var dynamicEntity = (target as DynamicEntity)!;

				EditorGUILayout.LabelField("Runtime Info");
				EditorGUI.indentLevel += 1;	
				EditorGUILayout.PrefixLabel("Entity ID");
				EditorGUILayout.SelectableLabel(dynamicEntity.entityId.ToString());
				EditorGUI.indentLevel -= 1;
			}
		}
	}
}
