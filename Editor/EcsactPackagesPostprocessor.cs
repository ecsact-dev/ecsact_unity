using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Ecsact.Editor;

public class EcsactPackagesPostprocessor : AssetPostprocessor {
	private struct MovedPkg {
		public string to;
		public string from;
	}

	static IEnumerable<(EcsactPackage, string)> FindEcsactPackages() {
		var guids = AssetDatabase.FindAssets($"t:{typeof(EcsactPackage)}");
		foreach(var t in guids) {
			var assetPath = AssetDatabase.GUIDToAssetPath(t);
			var asset = AssetDatabase.LoadAssetAtPath<EcsactPackage>(assetPath);
			if(asset != null) {
				yield return (asset, assetPath);
			}
		}
	}

	static void OnPostprocessAllAssets(
		string[] importedAssets,
		string[] deletedAssets,
		string[] movedAssets,
		string[] movedFromAssetPaths
	) {
		var importedPkgs = new List<string>();
		var deletedPkgs = new List<string>();
		var movedPkgs = new List<MovedPkg>();

		foreach(var importedAsset in importedAssets) {
			if(Path.GetExtension(importedAsset) == ".ecsact") {
				importedPkgs.Add(importedAsset);
			}
		}

		foreach(var deletedAsset in deletedAssets) {
			if(Path.GetExtension(deletedAsset) == ".ecsact") {
				deletedPkgs.Add(deletedAsset);
			}
		}

		for(int i = 0; movedAssets.Length > i; ++i) {
			if(Path.GetExtension(movedAssets[i]) == ".ecsact") {
				movedPkgs.Add(new MovedPkg {
					to = movedAssets[i],
					from = movedFromAssetPaths[i],
				});
			}
		}

		if(importedPkgs.Count > 0 || deletedPkgs.Count > 0 || movedPkgs.Count > 0) {
			RefreshEcsactCodegen(importedPkgs, deletedPkgs, movedPkgs);
		}
	}

	static void TryImportAssets(List<string> assets) {
		EditorApplication.delayCall += () => {
			if(!Progress.running) {
				try {
					AssetDatabase.StartAssetEditing();
					foreach(var asset in assets) {
						AssetDatabase.ImportAsset(asset);
					}
				} finally {
					AssetDatabase.StopAssetEditing();
				}
			} else {
				TryImportAssets(assets);
			}
		};
	}

	static void RefreshEcsactCodegen(
		List<string>   importedPkgs,
		List<string>   deletedPkgs,
		List<MovedPkg> movedPkgs
	) {
		string ecsactExecutable = EcsactSdk.FindExecutable("ecsact");

		var progressId = Progress.Start("Ecsact Codegen", "Generating C# files...");

		foreach(var movedPkg in movedPkgs) {
			AssetDatabase.MoveAsset(movedPkg.from + ".cs", movedPkg.to + ".cs");
		}

		foreach(var deletedPkg in deletedPkgs) {
			AssetDatabase.DeleteAsset(deletedPkg + ".cs");
		}

		Process codegen = new Process();
		codegen.StartInfo.FileName = ecsactExecutable;
		codegen.StartInfo.CreateNoWindow = true;
		codegen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		codegen.EnableRaisingEvents = true;
		codegen.StartInfo.Arguments = "codegen --plugin=csharp ";
		codegen.StartInfo.RedirectStandardError = true;
		codegen.StartInfo.RedirectStandardOutput = true;
		codegen.StartInfo.UseShellExecute = false;

		codegen.Exited += (_, _) => {
			if(codegen.ExitCode != 0) {
				UnityEngine.Debug.LogError(codegen.StandardError.ReadToEnd());
				Progress.Remove(progressId);
			} else {
				Progress.Finish(progressId, Progress.Status.Succeeded);
				// Import newly created scripts
				TryImportAssets(importedPkgs.Select(pkg => pkg + ".cs").ToList());
			}
		};

		var packages = FindEcsactPackages().ToList();

		foreach(var (pkg, pkgPath) in packages) {
			codegen.StartInfo.Arguments += pkgPath + " ";
		}

		Progress.Report(progressId, 0.1f);
		codegen.Start();

		EcsactRuntimeBuilder.Build(new EcsactRuntimeBuilder.Options {
			ecsactFiles = packages.Select(item => item.Item2).ToList(),
		});
	}
}
