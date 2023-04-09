using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Ecsact.Editor;

public class EcsactBenchmarkWindow : EditorWindow {
	private static int _progressId = 0;
	private static string _runtimePath {
		get => SessionState.GetString("ecsactBenchmarkRuntimePath", "");
		set { SessionState.SetString("ecsactBenchmarkRuntimePath", value); }
	}
	private static string _seedPath {
		get => SessionState.GetString("ecsactBenchmarkSeedPath", "");
		set { SessionState.SetString("ecsactBenchmarkSeedPath", value); }
	}

	[MenuItem("Window/Ecsact/Benchmark")]
	static void Init() {
		var window = EditorWindow.GetWindow(typeof(EcsactBenchmarkWindow));
		var windowTitle = new GUIContent{};
		windowTitle.text = "Ecsact - Benchmark";
		window.titleContent = windowTitle;
		window.Show();
	}

	void OnEnable() {
	}

	void OnDisable() {
	}

	void OnGUI() {
		var runtimeSettings = EcsactRuntimeSettings.Get();

		if(runtimeSettings.systemImplSource != Ecsact.SystemImplSource.WebAssembly) {
			EditorGUILayout.HelpBox(
				"Ecsact benchmark only supports WebAssembly system implementations.",
				MessageType.Info
			);
			return;
		}

#if HAS_UNITY_WASM_PACKAGE
		var benchmarkRunning = BenchmarkInProgress();
	
		EditorGUI.BeginDisabledGroup(benchmarkRunning);

		_runtimePath = EditorGUILayout.TextField("Runtime Path", _runtimePath);
		_seedPath = EditorGUILayout.TextField("Seed Path", _seedPath);

		EditorGUI.EndDisabledGroup();

		EditorGUILayout.Space();

		var wasmRuntimeSettings = EcsactWasmRuntimeSettings.Get();
		
		EditorGUI.BeginDisabledGroup(true);
		foreach(var entry in wasmRuntimeSettings.wasmSystemEntries) {
			if(entry.wasmAsset == null) continue;
			GUILayout.BeginHorizontal();
			EditorGUILayout.ObjectField(
				obj: entry.wasmAsset,
				objType: typeof(WasmAsset),
				allowSceneObjects: false
			);

			EditorGUILayout.LabelField(entry.wasmExportName);
			GUILayout.EndHorizontal();
		}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.Space();

		if(!benchmarkRunning) {
			var canStartBenchmark =
				File.Exists(_runtimePath) &&
				File.Exists(_seedPath);
			
			EditorGUI.BeginDisabledGroup(!canStartBenchmark);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Start Benchmark")) {
				StartBenchmark();
			}
			GUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();
		} else {
			EditorGUI.ProgressBar(
				new Rect(3, 3, position.width - 6, 20),
				0,
				"Benchmark in progress"
			);
		}

#else
		EditorGUILayout.HelpBox(
			"Unity wasm package is not installed. Please see your Ecsact settings.",
			MessageType.Error
		);
#endif
	}

	private static bool BenchmarkInProgress() {
		if(_progressId == 0) return false;
		var status = Progress.GetStatus(_progressId);
		return status == Progress.Status.Running;
	}

	private void StartBenchmark() {
#if HAS_UNITY_WASM_PACKAGE
		var ecsactExecutablePath = EcsactSdk.FindExecutable("ecsact");
		
		if(Progress.Exists(_progressId)) {
			Progress.Remove(_progressId);
		}
		_progressId = Progress.Start("Ecsact Benchmark");

		var proc = new Process();
		proc.StartInfo.FileName = ecsactExecutablePath;
		proc.StartInfo.CreateNoWindow = true;
		proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		proc.EnableRaisingEvents = true;
		proc.StartInfo.Arguments = "benchmark";
		proc.StartInfo.RedirectStandardError = true;
		proc.StartInfo.RedirectStandardOutput = true;
		proc.StartInfo.UseShellExecute = false;

		proc.StartInfo.Arguments += " --runtime=" + Path.GetFullPath(_runtimePath);
		proc.StartInfo.Arguments += " --seed=" + Path.GetFullPath(_seedPath);

		proc.ErrorDataReceived += (_, ev) => {
			try {
				var line = ev.Data;
				if(!string.IsNullOrWhiteSpace(line)) {
					UnityEngine.Debug.LogError(line);
					Progress.SetDescription(_progressId, line);
				}
			} catch(System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		};

		proc.Exited += (_, _) => {
			if(proc.ExitCode != 0) {
				Progress.Finish(_progressId, Progress.Status.Failed);
			} else {
				Progress.Finish(_progressId, Progress.Status.Succeeded);
			}
		};

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();
#endif
	}
}
