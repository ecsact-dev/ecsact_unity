using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ecsact {
	public class ExecutionOptions {

		public EcsactRuntime.CExecutionOptions executionOptions;
		private List<EcsactRuntime.EcsactAction> actions;

		internal ExecutionOptions() {
			actions = new();
			executionOptions = new();
		}

		public void AddActions() {
			var actionsArray = actions.ToArray();

			executionOptions.actions = actionsArray;
			executionOptions.actionsLength = actionsArray.Length;
		}

		public int actionCount() {
			return actions.Count;
		}

		public void PushAction<T>
			( T action
			) where T : Ecsact.Action
		{
			var actionId = Ecsact.Util.GetActionID<T>();
			var actionPtr = Marshal.AllocHGlobal(Marshal.SizeOf(action));
			Marshal.StructureToPtr(action, actionPtr, false);
			var ecsAction = new EcsactRuntime.EcsactAction {
				actionId = actionId,
				actionData = actionPtr
			};
			actions.Add(ecsAction);
		}

		public EcsactRuntime.CExecutionOptions C() {
			return executionOptions;
		}

		public void FreeActions() {
			foreach(var ecsAction in actions) {
				Marshal.FreeHGlobal(ecsAction.actionData);
			}
			actions.Clear();
		}

	};
}
