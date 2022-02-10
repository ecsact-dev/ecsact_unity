using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class EcsactPackagesPostprocessor : AssetPostprocessor {

	private struct MovedPkg {
		public string to;
		public string from;
	}

	static IEnumerable<(EcsactPackage, string)> FindEcsactPackages() {
		var guids = AssetDatabase.FindAssets($"t:{typeof(EcsactPackage)}");
		foreach (var t in guids) {
			var assetPath = AssetDatabase.GUIDToAssetPath(t);
			var asset = AssetDatabase.LoadAssetAtPath<EcsactPackage>(assetPath);
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
			if(Path.GetExtension(importedAsset) == ".ecsact") {
				importedPkgs.Add(importedAsset);
			}
		}

		foreach(var deletedAsset in deletedAssets) {
			if(Path.GetExtension(deletedAsset) == ".ecsact") {
				deletedPkgs.Add(deletedAsset);
			}
		}

		for (int i=0; movedAssets.Length > i; ++i) {
			if(Path.GetExtension(movedAssets[i]) == ".ecsact") {
				movedPkgs.Add(new MovedPkg{
					to = movedAssets[i],
					from = movedFromAssetPaths[i],
				});
			}
		}

		if(importedPkgs.Count > 0 || deletedPkgs.Count > 0 || movedPkgs.Count > 0) {
			RefreshEcsactCodegen(importedPkgs, deletedPkgs, movedPkgs);
		}
	}

	static void RefreshEcsactCodegen
		( List<string>    importedPkgs
		, List<string>    deletedPkgs
		, List<MovedPkg>  movedPkgs
		)
	{
		string csharpCodegenExecutable = Path.GetFullPath(
			"Packages/com.seaube.ecsact/generators~/ecsact_csharp_codegen.exe"
		);

		var progressId = Progress.Start(
			"ECSACT Codegen",
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

		foreach(var (pkg, pkgPath) in FindEcsactPackages()) {
			codegen.StartInfo.Arguments += pkgPath + " ";
		}

		Progress.Report(progressId, 0.1f);
		codegen.Start();
		
		foreach(var plugin in GetCodegenPlugins()) {
			// TODO: Custom codegen plugins
		}
	}

	static IEnumerable<EcsactCodegenPlugin> GetCodegenPlugins() {
		var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

		foreach(var assembly in assemblies) {
			foreach(var type in assembly.GetTypes()) {
				var pluginAttrs = type.GetCustomAttributes(
					typeof(EcsactCodegenPluginAttribute),
					inherit: true
				);

				if(pluginAttrs.Length > 0) {
					var pluginAttr = pluginAttrs[0] as EcsactCodegenPluginAttribute;
					var plugin =
						System.Activator.CreateInstance(type) as EcsactCodegenPlugin;
					if(plugin == null) {
						UnityEngine.Debug.LogError(
							$"Invalid EcsactCodegenPlugin: {pluginAttr!.name}"
						);
					} else {
						yield return plugin;
					}
				}
			}
		}
	}
}
