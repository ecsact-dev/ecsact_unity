using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Ecsact.Editor;

[System.Serializable]
class PkgInfoJson {
	public string name = "";
	public bool main = false;
	public List<string> imports = new();
	public List<EcsactPackage.Component> components = new();
}

[ScriptedImporter(version: 1, ext: "ecsact")]
public class EcsactImporter : ScriptedImporter {
	public override void OnImportAsset(AssetImportContext ctx) {
		string ecsactExecutable = EcsactSdk.FindExecutable("ecsact");

		var allEcsactFiles = Directory.GetFiles(
			path: "Assets",
			searchPattern: "*.ecsact",
			SearchOption.AllDirectories
		).ToHashSet();
		allEcsactFiles.Remove(ctx.assetPath);

		Process codegen = new Process();
		codegen.StartInfo.FileName = ecsactExecutable;
		codegen.StartInfo.CreateNoWindow = true;
		codegen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		codegen.EnableRaisingEvents = true;
		codegen.StartInfo.Arguments =
			"codegen " + 
			ctx.assetPath + " " +
			System.String.Join(" ", allEcsactFiles) +
			" --plugin=json" +
			" --stdout";
		codegen.StartInfo.RedirectStandardError = true;
		codegen.StartInfo.RedirectStandardOutput = true;
		codegen.StartInfo.UseShellExecute = false;

		var pkgJsonStr = "";
		var errMessage = "";

		codegen.ErrorDataReceived += (_, ev) => {
			if(ev.Data != null) {
				errMessage += ev.Data + "\n";
			}
		};

		codegen.OutputDataReceived  += (_, ev) => {
			if(ev.Data != null) {
				pkgJsonStr += ev.Data + "\n";
			}
		};
		
		try {
			codegen.Start();
			codegen.BeginOutputReadLine();
			codegen.BeginErrorReadLine();
			if(!codegen.WaitForExit(10000)) {
				ctx.LogImportError("Ecsact Importer timed out");
				return;
			} else {
				// See documentation https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-6.0#system-diagnostics-process-waitforexit(system-int32)
				codegen.WaitForExit();
			}
		} catch(System.Exception err) {
			ctx.LogImportError(err.Message);
			return;
		}

		if(codegen.ExitCode != 0) {
			ctx.LogImportError(errMessage);
			return;
		}

		var pkgJson = JsonUtility.FromJson<PkgInfoJson>(pkgJsonStr);
		var pkg = (EcsactPackage)ScriptableObject.CreateInstance(
			typeof(EcsactPackage)
		);

		pkg._name = pkgJson.name;
		pkg._imports = pkgJson.imports;
		pkg._components = pkgJson.components;

		ctx.AddObjectToAsset("ecsact package", pkg);
		ctx.SetMainObject(pkg);
	}
}
