using Unity.VisualScripting;
using UnityEngine;
using System;

namespace Ecsact.VisualScripting {
	[Serializable]
	public class AsyncErrorEventData {
		public Ecsact.AsyncError error;
		public Int32 requestId;
	}

	[UnitTitle("On Error")]
	[UnitCategory("Ecsact\\Async")]
	[UnitSurtitle("Ecsact / Async")]
	public class AsyncErrorEvent : EventUnit<AsyncErrorEventData> {
		public const string eventName = "EcsactAsyncErrorEvent";

		[PortLabel("Error")]
		[DoNotSerialize]
		public ValueOutput errorOutput { get; private set; }

		[PortLabel("Request ID")]
		[DoNotSerialize]
		public ValueOutput requestIdOutput { get; private set; }

		protected override bool register => true;

		public override EventHook GetHook
			( GraphReference reference
			)
		{
			return new EventHook(eventName);
		}

		protected override void Definition() {
			base.Definition();

			errorOutput = ValueOutput<Ecsact.AsyncError>(nameof(errorOutput));
			requestIdOutput = ValueOutput<Int32>(nameof(requestIdOutput));
		}

		protected override void AssignArguments
			( Flow                 flow
			, AsyncErrorEventData  data
			)
		{
			flow.SetValue(errorOutput, data.error);
			flow.SetValue(requestIdOutput, data.requestId);
		}
	}
}
