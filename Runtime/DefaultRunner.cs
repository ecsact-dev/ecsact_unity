using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable enable

namespace Ecsact {

[AddComponentMenu("")]
public class DefaultRunner : EcsactRunner {
	void Update() {
		Execute();
	}
}

} // namespace Ecsact
