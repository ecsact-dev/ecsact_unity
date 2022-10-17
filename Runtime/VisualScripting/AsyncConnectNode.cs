using Unity.VisualScripting;
using UnityEngine;

namespace Ecsact.VisualScripting {
	[UnitTitle("Connect")]
	[UnitCategory("Ecsact\\Async")]
	[UnitSurtitle("Ecsact / Async")]
	public class AsyncConnectNode : Unit {
		[PortLabelHidden]
		[DoNotSerialize]
		public ControlInput controlInput;

		[PortLabelHidden]
		[DoNotSerialize]
		public ControlOutput controlOutput;

		[DoNotSerialize]
		public ValueInput connectionStringInput;

		protected override void Definition() {
			connectionStringInput = ValueInput<string>("connectionString");
			controlOutput = ControlOutput("controlOutput");
			controlInput = ControlInput("controlInput", flow => {
				var connectUri = flow.GetValue<string>(connectionStringInput);
				Ecsact.Defaults._Runtime.async.Connect(connectUri);
				return controlOutput;
			});

			Requirement(connectionStringInput, controlInput);
			Succession(controlInput, controlOutput);
		}
	}
}
