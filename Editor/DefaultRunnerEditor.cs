using UnityEditor;
using UnityEngine;

namespace Ecsact.Editor {

[CustomEditor(typeof(Ecsact.DefaultRunner))]
public class DefaultRunnerEditor : UnityEditor.Editor {
	public override bool RequiresConstantRepaint() {
		return Application.isPlaying;
	}

	public override void OnInspectorGUI() {
		var runner = target as DefaultRunner;
		var                 executionHeatStyle = new GUIStyle(EditorStyles.label);
		executionHeatStyle.normal.textColor = Color.green;

		float deltaTimePc = 0f;
		if(runner.debugExecutionTimeMs > 0) {
			// 0.0 - 1.0 how much the execution time takes up from the delta time
			deltaTimePc =
				((float)runner.debugExecutionTimeMs / 1000f) / Time.deltaTime;

			if(deltaTimePc > 0.5) {
				executionHeatStyle.normal.textColor = Color.red;
			} else if(deltaTimePc > 0.2) {
				executionHeatStyle.normal.textColor = Color.yellow;
			}
		}

		EditorGUILayout.LabelField(
			"Execution Count",
			$"{runner.debugExecutionCountTotal}"
		);
		EditorGUILayout.LabelField(
			"Execution Time",
			$"{runner.debugExecutionTimeMs}ms " +
				$"({deltaTimePc*100:0.0}% of delta time)",
			executionHeatStyle
		);
	}
}

} // namespace Ecsact.Editor
