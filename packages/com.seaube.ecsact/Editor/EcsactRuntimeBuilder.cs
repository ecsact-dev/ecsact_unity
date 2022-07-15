using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public static class EcsactRuntimeBuilder {

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

		string runtimeBuilderExecutablePath = Path.GetFullPath(
			"Packages/com.seaube.ecsact/generators~/ecsact-rtb.exe"
		);

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
				Progress.SetDescription(progressId, line);
				UnityEngine.Debug.Log(line);
			}
		};

		proc.Exited += (_, _) => {
			if(proc.ExitCode != 0) {
				UnityEngine.Debug.LogError(
					$"ecsact-rtb exited with code {proc.ExitCode}"
				);
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

		UnityEngine.Debug.Log(proc.StartInfo.FileName);
		UnityEngine.Debug.Log(proc.StartInfo.Arguments);
		UnityEngine.Debug.Log($"CWD: {System.IO.Directory.GetCurrentDirectory()}");

		Progress.Report(progressId, 0.1f);
		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();
	}
}
