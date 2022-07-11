using Unity.VisualScripting;
using UnityEngine;
using System;

namespace Ecsact.VisualScripting {
	public class AsyncConnectEventData {
		public string connectAddress;
		public Int32 connectPort;
	}

	[UnitTitle("On Connect")]
	[UnitCategory("Ecsact\\Async")]
	[UnitSurtitle("Ecsact / Async")]
	public class AsyncConnectEvent : EventUnit<AsyncConnectEventData> {
		public const string eventName = "EcsactAsyncConnectEvent";

		[PortLabel("Connect Address")]
		[DoNotSerialize]
		public ValueOutput connectAddressOutput { get; private set; }

		[PortLabel("Connect port")]
		[DoNotSerialize]
		public ValueOutput connectPortOutput { get; private set; }

		protected override bool register => true;

		public override EventHook GetHook
			( GraphReference reference
			)
		{
			return new EventHook(eventName);
		}

		protected override void Definition() {
			base.Definition();

			connectAddressOutput = ValueOutput<string>(nameof(connectAddressOutput));
			connectPortOutput = ValueOutput<Int32>(nameof(connectPortOutput));
		}

		protected override void AssignArguments
			( Flow                   flow
			, AsyncConnectEventData  data
			)
		{
			flow.SetValue(connectAddressOutput, data.connectAddress);
			flow.SetValue(connectPortOutput, data.connectPort);
		}
	}
}
