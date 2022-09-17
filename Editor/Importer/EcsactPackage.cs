using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcsactImporter")]
[assembly: InternalsVisibleTo("EcsactPackagesPostprocessor")]

public class EcsactPackage : ScriptableObject {
	[SerializeField]
	internal string _name = "";
	[SerializeField]
	internal List<string> _imports = new List<string>();

	public new string name => _name;
	public IList<string> imports => _imports.AsReadOnly();
}
