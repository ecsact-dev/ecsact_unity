using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcsactImporter")]
[assembly: InternalsVisibleTo("EcsactPackagesPostprocessor")]

public class EcsactPackage : ScriptableObject {
	[System.Serializable]
	public struct FieldTypeInfo {
		public System.Int32 length;
		public string type;
	}
	[System.Serializable]
	public struct FieldInfo {
		public string field_name;
		public FieldTypeInfo field_type;
	}

	[System.Serializable]
	public class Component {
		public System.Int32 id = -1;
		public string full_name = "";
		public List<FieldInfo> fields = new();
	}

	[SerializeField]
	internal string _name = "";
	[SerializeField]
	internal List<string> _imports = new List<string>();
	[SerializeField]
	internal List<Component> _components = new List<Component>();

	public new string name => _name;
	public IList<string> imports => _imports.AsReadOnly();
	public IList<Component> components => _components.AsReadOnly();

	[MenuItem("Assets/Create/Ecsact File", priority=101)]
	private static void CreateEcsactPackageAsset() {
		ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
			"Packages/dev.ecsact.unity/Editor/AssetTemplates/EcsactFileTemplate.txt",
			"NewEcsactFile.ecsact"
		);
	}
}
