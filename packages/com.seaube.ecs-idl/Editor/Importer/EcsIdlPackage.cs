using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcsIdlImporter")]
[assembly: InternalsVisibleTo("EcsIdlPackagesPostprocessor")]

public class EcsIdlPackage : ScriptableObject {
  [SerializeField]
  internal string _name = "";
  [SerializeField]
  internal bool _main = false;
  [SerializeReference]
  internal List<EcsIdlPackage> _dependencies = new List<EcsIdlPackage>();
  [SerializeField]
  internal List<string> _imports = new List<string>();

  public new string name => _name;
  public bool main => _main;
  public IList<EcsIdlPackage> dependencies => _dependencies.AsReadOnly();
  public IList<string> imports => _imports.AsReadOnly();
}
