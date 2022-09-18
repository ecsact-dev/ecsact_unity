using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Ecsact.Editor;

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
		public bool available;
	}

	public const string type = "module_methods";
	public string module_name;
	public Dictionary<string, MethodInfo> methods;
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
struct SubcommandEndMessage {
	public const string type = "subcommand_end";
	public long id;
	public int exit_code;
}

public static class EcsactRuntimeBuilder {
	private static Dictionary<long, int> _subcommandProgressIds = new();

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

		var settings = EcsactSettings.GetOrCreateSettings();
		if(string.IsNullOrWhiteSpace(settings.runtimeBuilderOutputPath)) {
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
			var line = ev.Data;
			if(!string.IsNullOrWhiteSpace(line)) {
				UnityEngine.Debug.LogError(line);
				Progress.SetDescription(progressId, line);
			}
		};

		proc.OutputDataReceived += (_, ev) => {
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
				}
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

		proc.StartInfo.Arguments += "--output=\"";
		proc.StartInfo.Arguments += Path.GetFullPath(
			settings.runtimeBuilderOutputPath
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

		if(!string.IsNullOrWhiteSpace(settings.runtimeBuilderCompilerPath)) {
			proc.StartInfo.Arguments += "--compiler_path=\"";
			proc.StartInfo.Arguments += Path.GetFullPath(
				settings.runtimeBuilderCompilerPath
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

	private static void ReceiveMessage
		( int                   progressId
		, ModuleMethodsMessage  message
		)
	{

	}

	private static void ReceiveMessage
		( int                     progressId
		, SubcommandStartMessage  message
		)
	{
		var subcommandProgressId = Progress.Start(
			name: System.IO.Path.GetFileName(message.executable),
			description: string.Join(" ", message.arguments),
			parentId: progressId
		);
		_subcommandProgressIds.Add(message.id, subcommandProgressId);
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
	}
}
