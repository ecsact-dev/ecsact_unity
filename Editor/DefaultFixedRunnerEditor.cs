using UnityEditor;
using UnityEngine;

namespace Ecsact.Editor {

[CustomEditor(typeof(Ecsact.DefaultFixedRunner))]
public class DefaultFixedRunnerEditor : UnityEditor.Editor {
	private float deltaTimePcMax = 0f;
	private float executionTimeMsMax = 0;

	public override bool RequiresConstantRepaint() {
		return Application.isPlaying;
	}

	public override void OnInspectorGUI() {
		var runner = target as DefaultFixedRunner;
		var                 executionHeatStyle = new GUIStyle(EditorStyles.label);
		executionHeatStyle.normal.textColor = Color.green;

		float deltaTimePc = 0f;
		if(runner.debugExecutionTimeMs > 0) {
			// 0.0 - 1.0 how much the execution time takes up from the delta time
			deltaTimePc =
				((float)runner.debugExecutionTimeMs / 1000f) / Time.deltaTime;

			deltaTimePcMax = global::System.MathF.Max(deltaTimePcMax, deltaTimePc);
			executionTimeMsMax = global::System.Math.Max(
				executionTimeMsMax,
				runner.debugExecutionTimeMs
			);

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
			$"{runner.debugExecutionTimeMs}ms\t\tupper={executionTimeMsMax}ms",
			executionHeatStyle
		);
		EditorGUILayout.LabelField(
			"Delta Time",
			$"{deltaTimePc*100:0.0}%\t\tupper={deltaTimePcMax*100:0.0}%",
			executionHeatStyle
		);

		if(GUILayout.Button("Reset Uppers")) {
			executionTimeMsMax = 0;
			deltaTimePcMax = 0;
		}
	}
}

} // namespace Ecsact.Editor
