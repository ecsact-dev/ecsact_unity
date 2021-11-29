using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

[System.Serializable]
class PkgInfoJson {
	public string name = "";
	public bool main = false;
	public List<string> imports = new List<string>();
}

[ScriptedImporter(version: 1, ext: "ecs-idl")]
public class EcsIdlImporter : ScriptedImporter {
	public override void OnImportAsset(AssetImportContext ctx) {
		string codegenExecutable = Path.GetFullPath(
			"Packages/com.seaube.ecs-idl/.tmp/ecs_idl_parser_info_codegen.exe"
		);

		Process codegen = new Process();
		codegen.StartInfo.FileName = codegenExecutable;
		codegen.StartInfo.CreateNoWindow = true;
		codegen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		codegen.EnableRaisingEvents = true;
		codegen.StartInfo.Arguments = ctx.assetPath + " --ignore_unknown_imports";
		codegen.StartInfo.RedirectStandardError = true;
		codegen.StartInfo.RedirectStandardOutput = true;
		codegen.StartInfo.UseShellExecute = false;

		try {
			codegen.Start();
			codegen.WaitForExit();
		} catch(System.Exception err) {
			ctx.LogImportError(err.Message);
			return;
		}

		if(codegen.ExitCode != 0) {
			ctx.LogImportError(codegen.StandardError.ReadToEnd());
			return;
		}

		var pkgJsonStr = codegen.StandardOutput.ReadToEnd();
		var pkgJson = JsonUtility.FromJson<PkgInfoJson>(pkgJsonStr);
		var pkg = (EcsIdlPackage)ScriptableObject.CreateInstance(
			typeof(EcsIdlPackage)
		);

		pkg._name = pkgJson.name;
		pkg._main = pkgJson.main;
		pkg._imports = pkgJson.imports;
		pkg._dependencies.Clear();

		ctx.AddObjectToAsset("ecs-idl package", pkg);
		ctx.SetMainObject(pkg);
	}
}
