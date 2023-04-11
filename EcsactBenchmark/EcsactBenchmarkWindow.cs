using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Ecsact.Editor;

[System.Serializable]
struct MessageBase {
	public string type;
}

[System.Serializable]
struct InfoMessage {
	public const string type = "info";
	public string       content;
}

[System.Serializable]
struct WarningMessage {
	public const string type = "warning";
	public string       content;
}

[System.Serializable]
struct ErrorMessage {
	public const string type = "error";
	public string       content;
}

[System.Serializable]
struct BenchmarkProgressMessage {
	public const string type = "progress";
	public float        progress;
}

[System.Serializable]
struct BenchmarkResultMessage {
	public const string type = "result";
	public float        total_duration_ms;
	public float        average_duration_ms;
}

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

	private static int _iterations {
		get => SessionState.GetInt("ecsactBenchmarkIterations", 10000);
		set { SessionState.SetInt("ecsactBenchmarkIterations", value); }
	}

	private static int _reportInterval {
		get => SessionState.GetInt("ecsactBenchmarkReportInterval", 1000);
		set { SessionState.SetInt("ecsactBenchmarkReportInterval", value); }
	}

	private static bool _benchmarkResultFoldout {
		get => SessionState.GetBool("ecsatBenchmarkResultFoldout", true);
		set { SessionState.SetBool("ecsatBenchmarkResultFoldout", value); }
	}

	private static GUIContent copyIcon;
	
	private float _currentProgress = 0.0F;
	private BenchmarkResultMessage? _lastResult = null;

	Vector2 scrollPosition = new();

	[MenuItem("Window/Ecsact/Benchmark")]
	static void Init() {
		var window = EditorWindow.GetWindow(typeof(EcsactBenchmarkWindow));
		var windowTitle = new GUIContent{};
		windowTitle.text = "Ecsact - Benchmark";
		window.titleContent = windowTitle;
		window.Show();
	}

	void OnEnable() {
		if(copyIcon == null) {
			copyIcon = EditorGUIUtility.IconContent("Clipboard");
		}
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
		
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		EditorGUI.BeginDisabledGroup(benchmarkRunning);

		_runtimePath = EditorGUILayout.TextField("Runtime Path", _runtimePath);
		_seedPath = EditorGUILayout.TextField("Seed Path", _seedPath);

		_iterations = EditorGUILayout.IntField("Iterations", _iterations);
		_reportInterval = EditorGUILayout.IntField("Report Interval", _reportInterval);

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

			if(GUILayout.Button(copyIcon)) {
				EditorGUIUtility.systemCopyBuffer =
					"ecsact benchmark " + GetBenchmarkArguments();
			}
			if(GUILayout.Button("Start Benchmark")) {
				StartBenchmark();
			}
			GUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();
		} else {
			var progressRect = GUILayoutUtility.GetLastRect();
			progressRect.position = new Vector2{
				x = progressRect.position.x,
				y = progressRect.position.y + 10,
			};
			progressRect.height = 20;
			EditorGUI.ProgressBar(
				progressRect,
				_currentProgress,
				"Benchmark in progress"
			);
		}

		if(_lastResult != null) {
			_benchmarkResultFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
				_benchmarkResultFoldout,
				"Benchmark Result"
			);
			if(_benchmarkResultFoldout) {
				EditorGUILayout.FloatField("    Average (ms)", _lastResult.Value.average_duration_ms);
				EditorGUILayout.FloatField("    Total (ms)", _lastResult.Value.total_duration_ms);
			}
		}
		
		EditorGUILayout.EndScrollView();

#else
		EditorGUILayout.HelpBox(
			"Unity wasm package is not installed. Please see your Ecsact settings.",
			MessageType.Error
		);
#endif
	}

	private static bool BenchmarkInProgress() {
		if(_progressId == 0) return false;
		if(!Progress.Exists(_progressId)) return false;
		var status = Progress.GetStatus(_progressId);
		return status == Progress.Status.Running;
	}

	private string GetBenchmarkArguments() {
		var benchmarkProcArgs = "";
#if HAS_UNITY_WASM_PACKAGE
		benchmarkProcArgs += " --runtime=" + Path.GetFullPath(_runtimePath).Replace('\\', '/');
		benchmarkProcArgs += " --seed=" + Path.GetFullPath(_seedPath).Replace('\\', '/');
		benchmarkProcArgs += " --iterations=" + _iterations;
		benchmarkProcArgs += " --iteration_report_interval=" + _reportInterval;
		
		var wasmRuntimeSettings = EcsactWasmRuntimeSettings.Get();
		foreach(var entry in wasmRuntimeSettings.wasmSystemEntries) {
			if(!entry.wasmAsset) continue;
			var assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.wasmAsset);
			if(string.IsNullOrEmpty(assetPath)) continue;
			assetPath = Path.GetFullPath(assetPath).Replace('\\', '/');

			var exportName = entry.wasmExportName;
			var systemId = entry.systemId;
			
			benchmarkProcArgs += $" \"{assetPath};{exportName},{systemId}\"";
		}
#endif
		return benchmarkProcArgs;
	}

	private void OnInfoMessage(InfoMessage msg) {
		UnityEngine.Debug.Log(msg.content);
	}

	private void OnWarningMessage(WarningMessage msg) {
		UnityEngine.Debug.LogWarning(msg.content);
	}

	private void OnErrorMessage(ErrorMessage msg) {
		UnityEngine.Debug.LogError(msg.content);
	}

	private void OnBenchmarkProgressMessage(BenchmarkProgressMessage msg) {
		_currentProgress = msg.progress;
	}

	private void OnBenchmarkResultMessage(BenchmarkResultMessage msg) {
		EditorApplication.delayCall += () => {
			_lastResult = msg;
		};
	}

	private void OnUnknownMessage(string type, string json) {
		UnityEngine.Debug.LogWarning($"Unknown benchmark message {type}: {json}");
	}

	private void OnBenchmarkMessage(string type, string json) {
		switch(type) {
			case InfoMessage.type: OnInfoMessage(JsonUtility.FromJson<InfoMessage>(json)); break;
			case WarningMessage.type: OnWarningMessage(JsonUtility.FromJson<WarningMessage>(json)); break;
			case ErrorMessage.type: OnErrorMessage(JsonUtility.FromJson<ErrorMessage>(json)); break;
			case BenchmarkProgressMessage.type: OnBenchmarkProgressMessage(JsonUtility.FromJson<BenchmarkProgressMessage>(json)); break;
			case BenchmarkResultMessage.type: OnBenchmarkResultMessage(JsonUtility.FromJson<BenchmarkResultMessage>(json)); break;
			default: OnUnknownMessage(type, json); break;
		}
	}

	void Update() {
		if(BenchmarkInProgress()) {
			Progress.Report(_progressId, _currentProgress);
			Repaint();
		}
	}

	private void StartBenchmark() {
		_lastResult = null;
		_currentProgress = 0.0F;

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
		proc.StartInfo.Arguments = "benchmark " + GetBenchmarkArguments();
		proc.StartInfo.RedirectStandardError = true;
		proc.StartInfo.RedirectStandardOutput = true;
		proc.StartInfo.UseShellExecute = false;
		
		var wasmRuntimeSettings = EcsactWasmRuntimeSettings.Get();
		foreach(var entry in wasmRuntimeSettings.wasmSystemEntries) {
			if(!entry.wasmAsset) continue;
			var assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.wasmAsset);
			if(string.IsNullOrEmpty(assetPath)) continue;
			assetPath = Path.GetFullPath(assetPath);

			var exportName = entry.wasmExportName;
			var systemId = entry.systemId;
			
			proc.StartInfo.Arguments += $" {assetPath};{exportName},{systemId}";
		}

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

			EditorApplication.delayCall += () => Repaint();
		};

		proc.OutputDataReceived += (_, ev) => {
			try {
				var line = ev.Data;
				if(!string.IsNullOrWhiteSpace(line)) {
					var baseMessage = JsonUtility.FromJson<MessageBase>(line);
					OnBenchmarkMessage(baseMessage.type, line);
				}
			} catch(global::System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		};

		Progress.RegisterCancelCallback(_progressId, () => {
			proc.Close();
			EditorApplication.delayCall += () => Repaint();
			return true;
		});

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();
#endif
	}
}
