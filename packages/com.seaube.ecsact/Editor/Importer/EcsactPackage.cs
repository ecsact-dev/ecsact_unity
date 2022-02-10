using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcsactImporter")]
[assembly: InternalsVisibleTo("EcsactPackagesPostprocessor")]

public class EcsactPackage : ScriptableObject {
  [SerializeField]
  internal string _name = "";
  [SerializeField]
  internal bool _main = false;
  [SerializeReference]
  internal List<EcsactPackage> _dependencies = new List<EcsactPackage>();
  [SerializeField]
  internal List<string> _imports = new List<string>();

  public new string name => _name;
  public bool main => _main;
  public IList<EcsactPackage> dependencies => _dependencies.AsReadOnly();
  public IList<string> imports => _imports.AsReadOnly();
}
