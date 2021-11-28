using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using System.Diagnostics;

[ScriptedImporter(version: 1, ext: "ecs-idl")]
public class EcsIdlImporter : ScriptedImporter {
	public override void OnImportAsset(AssetImportContext ctx) {
		string csharpCodegenExecutable = Path.GetFullPath(
			"Packages/com.seaube.ecs-idl/.tmp/ecs_idl_csharp_codegen.exe"
		);

		Process codegen = new Process();
		codegen.StartInfo.FileName = csharpCodegenExecutable;
		codegen.StartInfo.CreateNoWindow = true;
		codegen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		codegen.EnableRaisingEvents = true;
		codegen.StartInfo.Arguments = ctx.assetPath;
		codegen.StartInfo.RedirectStandardError = true;
		codegen.StartInfo.UseShellExecute = false;
		codegen.Start();
		codegen.WaitForExit();

		if(codegen.ExitCode != 0) {
			StreamReader stderr = codegen.StandardError;
			string line;
			string errMessage = "";
			while((line = stderr.ReadLine()) != null) {
				errMessage += line + "\n";
			}
			ctx.LogImportError(errMessage);
			return;
		}

		var scriptPath = ctx.assetPath + ".cs";

		File.SetAttributes(
			scriptPath,
			File.GetAttributes(scriptPath) | FileAttributes.ReadOnly
		);

		AssetDatabase.ImportAsset(scriptPath);
	}
}
