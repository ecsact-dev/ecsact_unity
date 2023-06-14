using Unity.VisualScripting;
using UnityEngine;

namespace Ecsact.VisualScripting {

[UnitTitle("On Async Connect State Change")]
[UnitCategory("Events\\Ecsact")]
public class AsyncConnectStateChangeEvent
	: EventUnit<Ecsact.Async.ConnectState> {
	public const string eventName = "EcsactAsyncConnectStateChange";

	[RuntimeInitializeOnLoadMethod]
	private static void OnLoad() {
		Ecsact.Defaults.WhenReady(OnEcsactRuntimeReady);
	}

	private static void OnEcsactRuntimeReady() {
		Ecsact.Defaults.Runtime.async.connectStateChange += OnConnectStateChange;
	}

	private static void OnConnectStateChange(Ecsact.Async.ConnectState state) {
		EventBus.Trigger(eventName, state);
	}

	[DoNotSerialize]
	public ValueOutput NewConnectState { get; private set; }
	protected override bool register => true;

	public override EventHook GetHook(GraphReference reference) {
		return new EventHook(eventName);
	}

	protected override void Definition() {
		base.Definition();
		NewConnectState = ValueOutput<Ecsact.Async.ConnectState>( //
			nameof(NewConnectState)
		);
	}

	protected override void AssignArguments(
		Flow                      flow,
		Ecsact.Async.ConnectState data
	) {
		flow.SetValue(NewConnectState, data);
	}
}
}
