using UnityEditor;
using UnityEngine;
using System.IO;

#nullable enable

namespace Ecsact.Editor {

public class CsharpSystemImplSettings : ScriptableObject {
	private static CsharpSystemImplSettings? _instance;
	public const string assetPath =
		"Assets/Editor/EcsactCsharpSystemImplSettings.asset";

	[Tooltip("The assembly that contains all the Ecsacts sytem impls")]
	public UnityEditorInternal.AssemblyDefinitionAsset? systemImplsAssembly;

	public static CsharpSystemImplSettings Get() {
		if(_instance != null) {
			return _instance;
		}

		_instance =
			AssetDatabase.LoadAssetAtPath<CsharpSystemImplSettings>(assetPath);
		if(_instance == null) {
			_instance = ScriptableObject.CreateInstance<CsharpSystemImplSettings>();
			Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
			AssetDatabase.CreateAsset(_instance, assetPath);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}

		if(_instance == null) {
			throw new global::System.Exception(
				"Failed to load CsharpSystemImplSettings"
			);
		}

		return _instance;
	}
}

} // namespace Ecsact.Editor
