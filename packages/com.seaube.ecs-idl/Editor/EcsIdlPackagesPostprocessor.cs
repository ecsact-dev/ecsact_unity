using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class EcsIdlPackagesPostprocessor : AssetPostprocessor {

	private struct MovedPkg {
		public string to;
		public string from;
	}

	static IEnumerable<(EcsIdlPackage, string)> FindEcsIdlPackages() {
		var guids = AssetDatabase.FindAssets($"t:{typeof(EcsIdlPackage)}");
		foreach (var t in guids) {
			var assetPath = AssetDatabase.GUIDToAssetPath(t);
			var asset = AssetDatabase.LoadAssetAtPath<EcsIdlPackage>(assetPath);
			if (asset != null) {
				yield return (asset, assetPath);
			}
		}
	}

	static void OnPostprocessAllAssets
		( string[]  importedAssets
		, string[]  deletedAssets
		, string[]  movedAssets
		, string[]  movedFromAssetPaths
		)
	{
		var importedPkgs = new List<string>();
		var deletedPkgs = new List<string>();
		var movedPkgs = new List<MovedPkg>();

		foreach(var importedAsset in importedAssets) {
			if(Path.GetExtension(importedAsset) == ".ecs-idl") {
				importedPkgs.Add(importedAsset);
			}
		}

		foreach(var deletedAsset in deletedAssets) {
			if(Path.GetExtension(deletedAsset) == ".ecs-idl") {
				deletedPkgs.Add(deletedAsset);
			}
		}

		for (int i=0; movedAssets.Length > i; ++i) {
			if(Path.GetExtension(movedAssets[i]) == ".ecs-idl") {
				movedPkgs.Add(new MovedPkg{
					to = movedAssets[i],
					from = movedFromAssetPaths[i],
				});
			}
		}

		if(importedPkgs.Count > 0 || deletedPkgs.Count > 0 || movedPkgs.Count > 0) {
			RefreshEcsIdlCodegen(importedPkgs, deletedPkgs, movedPkgs);
		}
	}

	static void RefreshEcsIdlCodegen
		( List<string>    importedPkgs
		, List<string>    deletedPkgs
		, List<MovedPkg>  movedPkgs
		)
	{
		string csharpCodegenExecutable = Path.GetFullPath(
			"Packages/com.seaube.ecs-idl/.tmp/ecs_idl_csharp_codegen.exe"
		);

		var progressId = Progress.Start(
			"ECS IDL Codegen",
			"Generating C# files..."
		);

		foreach(var movedPkg in movedPkgs) {
			AssetDatabase.MoveAsset(movedPkg.from + ".cs", movedPkg.to + ".cs");
		}

		foreach(var deletedPkg in deletedPkgs) {
			AssetDatabase.DeleteAsset(deletedPkg + ".cs");
		}

		Process codegen = new Process();
		codegen.StartInfo.FileName = csharpCodegenExecutable;
		codegen.StartInfo.CreateNoWindow = true;
		codegen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		codegen.EnableRaisingEvents = true;
		codegen.StartInfo.Arguments = "";
		codegen.StartInfo.RedirectStandardError = true;
		codegen.StartInfo.RedirectStandardOutput = true;
		codegen.StartInfo.UseShellExecute = false;

		codegen.Exited += (_, _) => {
			if(codegen.ExitCode != 0) {
				UnityEngine.Debug.LogError(codegen.StandardError.ReadToEnd());
				Progress.Remove(progressId);
			} else {
				Progress.Report(progressId, 0.9f);

				EditorApplication.delayCall += () => {
					foreach(var importedPkg in importedPkgs) {
						// Import newly created scripts
						AssetDatabase.ImportAsset(importedPkg + ".cs");
					}
					Progress.Remove(progressId);
				};
			}
		};

		foreach(var (pkg, pkgPath) in FindEcsIdlPackages()) {
			codegen.StartInfo.Arguments += pkgPath + " ";
		}

		Progress.Report(progressId, 0.1f);
		codegen.Start();
	}
}
