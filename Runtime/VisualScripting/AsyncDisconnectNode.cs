using Unity.VisualScripting;
using UnityEngine;

namespace Ecsact.VisualScripting {

[UnitTitle("Disconnect")]
[UnitCategory("Ecsact\\Async")]
[UnitSurtitle("Ecsact / Async")]
public class AsyncDisconnectNode : Unit {
	[PortLabelHidden]
	[DoNotSerialize]
	public ControlInput controlInput;

	[PortLabelHidden]
	[DoNotSerialize]
	public ControlOutput controlOutput;

	protected override void Definition() {
		controlOutput = ControlOutput("controlOutput");
		controlInput = ControlInput("controlInput", flow => {
			Ecsact.Defaults.Runtime.async.Disconnect();
			return controlOutput;
		});

		Succession(controlInput, controlOutput);
	}
}

}
