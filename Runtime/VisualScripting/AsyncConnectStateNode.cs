
using Unity.VisualScripting;
using UnityEngine;

namespace Ecsact.VisualScripting {

[UnitTitle("Get Connect State")]
[UnitCategory("Ecsact\\Async")]
[UnitSurtitle("Ecsact / Async")]
public class AsyncConnectStateNode : Unit {
	[DoNotSerialize]
	public ValueOutput connectStateOutput;

	protected override void Definition() {
		connectStateOutput = ValueOutput<Ecsact.Async.ConnectState>(
			"connectState",
			_ => Ecsact.Defaults.Runtime.async.connectState
		);
	}
}

}
