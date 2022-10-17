using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Ecsact.Editor;

#nullable enable

[System.Serializable]
struct MessageBase {
	public string type;
}

[System.Serializable]
struct AlertMessage {
	public const string type = "alert";
	public string content;
}

[System.Serializable]
struct InfoMessage {
	public const string type = "info";
	public string content;
}

[System.Serializable]
struct ErrorMessage {
	public const string type = "error";
	public string content;
}

[System.Serializable]
struct EcsactErrorMessage {
	public const string type = "ecsact_error";
	public string ecsact_source_path;
	public string message;
	public global::System.Int32 line;
	public global::System.Int32 character;
}

[System.Serializable]
struct WarningMessage {
	public const string type = "warning";
	public string content;
}

[System.Serializable]
struct SuccessMessage {
	public const string type = "success";
	public string content;
}

[System.Serializable]
struct ModuleMethodsMessage {
	[System.Serializable]
	public struct MethodInfo {
		public string method_name;
		public bool available;
	}

	public const string type = "module_methods";
	public string module_name;
	public List<MethodInfo> methods;
}

[System.Serializable]
struct SubcommandStartMessage {
	public const string type = "subcommand_start";
	public long id;
	public string executable;
	public List<string> arguments;
}

[System.Serializable]
struct SubcommandProgressMessage {
	public const string type = "subcommand_progress";
	public long id;
	public string description;
}

[System.Serializable]
struct SubcommandStdoutMessage {
	public const string type = "subcommand_stdout";
	public long id;
	public string line;
}

[System.Serializable]
struct SubcommandStderrMessage {
	public const string type = "subcommand_stderr";
	public long id;
	public string line;
}

[System.Serializable]
struct SubcommandEndMessage {
	public const string type = "subcommand_end";
	public long id;
	public int exit_code;
}

public static class EcsactRuntimeBuilder {
	private static Dictionary<long, int> _subcommandProgressIds = new();
	private static Dictionary<long, string> _subcommandProgressNames = new();
	private static EcsactSettings? _settings;

	public struct Options {
		public List<string> ecsactFiles;
	}

	public static void Build
		( Options options
		)
	{
		if(options.ecsactFiles.Count == 0) {
			UnityEngine.Debug.LogError(
				"Cannot build ecsact runtime without any ecsact files"
			);
			return;
		}

		_settings = EcsactSettings.GetOrCreateSettings();
		if(string.IsNullOrWhiteSpace(_settings.runtimeBuilderOutputPath)) {
			UnityEngine.Debug.LogError(
				"Cannot build ecsact runtime without output path"
			);
			return;
		}

		string runtimeBuilderExecutablePath =
			EcsactSdk.FindExecutable("ecsact_rtb");

		var progressId = Progress.Start("Ecsact Runtime Builder");

		var proc = new Process();
		proc.StartInfo.FileName = runtimeBuilderExecutablePath;
		proc.StartInfo.CreateNoWindow = true;
		proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		proc.EnableRaisingEvents = true;
		proc.StartInfo.Arguments = "";
		proc.StartInfo.RedirectStandardError = true;
		proc.StartInfo.RedirectStandardOutput = true;
		proc.StartInfo.UseShellExecute = false;

		proc.ErrorDataReceived += (_, ev) => {
			try {
				var line = ev.Data;
				if(!string.IsNullOrWhiteSpace(line)) {
					UnityEngine.Debug.LogError(line);
					Progress.SetDescription(progressId, line);
				}
			} catch(System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		};

		proc.OutputDataReceived += (_, ev) => {
			try {
				var line = ev.Data;
				if(!string.IsNullOrWhiteSpace(line)) {
					var baseMessage = JsonUtility.FromJson<MessageBase>(line);
					switch(baseMessage.type) {
						case AlertMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<AlertMessage>(line)
							);
							break;
						case InfoMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<InfoMessage>(line)
							);
							break;
						case ErrorMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<ErrorMessage>(line)
							);
							break;
						case EcsactErrorMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<EcsactErrorMessage>(line)
							);
							break;
						case WarningMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<WarningMessage>(line)
							);
							break;
						case SuccessMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SuccessMessage>(line)
							);
							break;
						case ModuleMethodsMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<ModuleMethodsMessage>(line)
							);
							break;
						case SubcommandStartMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SubcommandStartMessage>(line)
							);
							break;
						case SubcommandProgressMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SubcommandProgressMessage>(line)
							);
							break;
						case SubcommandEndMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SubcommandEndMessage>(line)
							);
							break;
						case SubcommandStdoutMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SubcommandStdoutMessage>(line)
							);
							break;
						case SubcommandStderrMessage.type:
							ReceiveMessage(
								progressId,
								JsonUtility.FromJson<SubcommandStderrMessage>(line)
							);
							break;
						default:
							UnityEngine.Debug.LogWarning(
								$"Unhandled Ecsact Runtime Builder Message: {baseMessage.type}"
							);
							break;
					}
				}
			} catch(System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		};

		proc.Exited += (_, _) => {
			if(proc.ExitCode != 0) {
				Progress.Finish(progressId, Progress.Status.Failed);
			} else {
				Progress.Finish(progressId, Progress.Status.Succeeded);
			}
		};

		foreach(var ecsactFile in options.ecsactFiles) {
			proc.StartInfo.Arguments += "\"" + ecsactFile + "\" ";
		}

		if(_settings.runtimeBuilderDebugBuild) {
			proc.StartInfo.Arguments += " --debug ";
		}

		proc.StartInfo.Arguments += "--output=\"";
		proc.StartInfo.Arguments += Path.GetFullPath(
			_settings.runtimeBuilderOutputPath
		);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		proc.StartInfo.Arguments += ".dll";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
		proc.StartInfo.Arguments += ".so";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		proc.StartInfo.Arguments += ".dylib";
#elif UNITY_WEBGL
		proc.StartInfo.Arguments += ".wasm";
#endif
		proc.StartInfo.Arguments += "\" ";

		if(!string.IsNullOrWhiteSpace(_settings.runtimeBuilderCompilerPath)) {
			proc.StartInfo.Arguments += "--compiler_path=\"";
			proc.StartInfo.Arguments += Path.GetFullPath(
				_settings.runtimeBuilderCompilerPath
			);
			proc.StartInfo.Arguments += "\" ";
		}

		proc.StartInfo.Arguments += "--temp_dir=";
		proc.StartInfo.Arguments += Path.GetFullPath(
			FileUtil.GetUniqueTempPathInProject()
		);

		Progress.Report(progressId, 0.1f);
		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();
	}

	private static void ReceiveMessage
		( int           progressId
		, AlertMessage  message
		)
	{
		EditorUtility.DisplayDialog(
			title: "Ecsact Runtime Builder",
			message: message.content,
			ok: "ok"
		);
	}

	private static void ReceiveMessage
		( int          progressId
		, InfoMessage  message
		)
	{
		Progress.SetDescription(progressId, message.content);
		UnityEngine.Debug.Log(message.content);
	}

	private static void ReceiveMessage
		( int           progressId
		, ErrorMessage  message
		)
	{
		Progress.SetDescription(progressId, message.content);
		UnityEngine.Debug.LogError(message.content);
	}

	private static void ReceiveMessage
		( int                 progressId
		, EcsactErrorMessage  message
		)
	{
		Progress.SetDescription(progressId, message.message);
		UnityEditor.EditorApplication.delayCall += () => {
			var ecsactAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(
				message.ecsact_source_path
			);
			UnityEngine.Debug.LogFormat(
				context: ecsactAsset,
				format: $"{message.ecsact_source_path}:{message.line}:{message.character} {message.message}",
				logType: LogType.Error,
				logOptions: LogOption.NoStacktrace
			);
		};
	}

	private static void ReceiveMessage
		( int             progressId
		, WarningMessage  message
		)
	{
		Progress.SetDescription(progressId, message.content);
		UnityEngine.Debug.LogWarning(message.content);
	}

	private static void ReceiveMessage
		( int             progressId
		, SuccessMessage  message
		)
	{
		Progress.SetDescription(progressId, message.content);
		UnityEngine.Debug.Log(message.content);
	}

	private static void CheckMethods
		( IEnumerable<string>   methods
		, ModuleMethodsMessage  message
		)
	{
		var methodsList = methods.ToList();

		foreach(var methodName in methods) {
			var methodInfoIndex = message.methods.FindIndex(
				v => v.method_name == methodName
			);
			if(methodInfoIndex == -1) {
				UnityEngine.Debug.LogWarning(
					$"Old method '{methodName}' should be <color=red>removed</color> " +
					$"from module <b>{message.module_name}</b>. It no longer exists. " + 
					"(reported by ecsact_rtb)"
				);
			}
		}

		foreach(var methodInfo in message.methods) {
			var methodName = methodInfo.method_name;
			if(!methods.Contains(methodName)) {
				UnityEngine.Debug.LogWarning(
					$"New method '{methodName}' should be <color=green>added</color> " +
					$"to module <b>{message.module_name}</b>. (reported by ecsact_rtb)"
				);
			}
		}
	}

	private static void ReceiveMessage
		( int                   progressId
		, ModuleMethodsMessage  message
		)
	{
		switch(message.module_name) {
			case "core":
				CheckMethods(EcsactRuntime.Core.methods, message);
				break;
			case "dynamic":
				CheckMethods(EcsactRuntime.Dynamic.methods, message);
				break;
			case "meta":
				CheckMethods(EcsactRuntime.Meta.methods, message);
				break;
			case "static":
				CheckMethods(EcsactRuntime.Static.methods, message);
				break;
			case "serialize":
				CheckMethods(EcsactRuntime.Serialize.methods, message);
				break;
		}
	}

	private static void ReceiveMessage
		( int                     progressId
		, SubcommandStartMessage  message
		)
	{
		var subcommandName = System.IO.Path.GetFileName(message.executable);
		var subcommandProgressId = Progress.Start(
			name: subcommandName,
			description: string.Join(" ", message.arguments),
			parentId: progressId
		);
		_subcommandProgressIds.Add(message.id, subcommandProgressId);
		_subcommandProgressNames.Add(message.id, subcommandName);
	}

	private static void ReceiveMessage
		( int                        progressId
		, SubcommandProgressMessage  message
		)
	{
		var subcommandProgressId = _subcommandProgressIds[message.id];
		Progress.SetDescription(subcommandProgressId, message.description);
	}

	private static void ReceiveMessage
		( int                      progressId
		, SubcommandStdoutMessage  message
		)
	{
		if(_settings!.runtimeBuilderPrintSubcommandStdout) {
			string name = "unknown";
			if(_subcommandProgressNames.ContainsKey(message.id)) {
				name = _subcommandProgressNames[message.id];
			}
			UnityEngine.Debug.Log(
				$"[{name} subcommand stdout] {message.line}"
			);
		}
	}

	private static void ReceiveMessage
		( int                      progressId
		, SubcommandStderrMessage  message
		)
	{
		if(_settings!.runtimeBuilderPrintSubcommandStderr) {
			string name = "unknown";
			if(_subcommandProgressNames.ContainsKey(message.id)) {
				name = _subcommandProgressNames[message.id];
			}
			UnityEngine.Debug.Log(
				$"[{name} subcommand <color=red>stderr</color>] {message.line}"
			);
		}
	}

	private static void ReceiveMessage
		( int                   progressId
		, SubcommandEndMessage  message
		)
	{
		var subcommandProgressId = _subcommandProgressIds[message.id];
		if(message.exit_code == 0) {
			Progress.Finish(subcommandProgressId, Progress.Status.Succeeded);
		} else {
			Progress.Finish(subcommandProgressId, Progress.Status.Failed);
		}

		_subcommandProgressIds.Remove(message.id);
		_subcommandProgressNames.Remove(message.id);
	}
}
