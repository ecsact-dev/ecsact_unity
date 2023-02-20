using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace Ecsact {

public class ExecutionOptions {
	public EcsactRuntime.CExecutionOptions executionOptions;

	private List<EcsactRuntime.EcsactAction>      actions;
	private List<EcsactRuntime.EcsactComponent>   adds;
	private List<Int32>                           adds_entities;
	private List<EcsactRuntime.EcsactComponent>   updates;
	private List<Int32>                           updates_entities;
	private List<EcsactRuntime.EcsactComponentId> removes;
	private List<Int32>                           removes_entities;

	internal ExecutionOptions() {
		actions = new();
		adds = new();
		adds_entities = new();
		updates = new();
		updates_entities = new();
		removes = new();
		removes_entities = new();
		executionOptions = new();
	}

	public void AddComponent<C>(Int32 entityId, C component)
		where     C : Ecsact.Component {
    var componentId = Ecsact.Util.GetComponentID<C>();
    var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

    Marshal.StructureToPtr(component, componentPtr, false);
    var ecsComponent = new EcsactRuntime.EcsactComponent {
      componentId = componentId,
      componentData = componentPtr
    };
    adds.Add(ecsComponent);
    adds_entities.Add(entityId);
	}

	public void AddComponent(
		Int32  entityId,
		Int32  componentId,
		object componentData
	) {
		var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(componentData));

		Marshal.StructureToPtr(componentId, componentPtr, false);
		var ecsComponent = new EcsactRuntime.EcsactComponent {
			componentId = componentId,
			componentData = componentPtr
		};
		adds.Add(ecsComponent);
		adds_entities.Add(entityId);
	}

	public void UpdateComponent<C>(Int32 entityId, C component)
		where     C : Ecsact.Component {
    var componentId = Ecsact.Util.GetComponentID<C>();
    var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

    Marshal.StructureToPtr(component, componentPtr, false);
    var ecsComponent = new EcsactRuntime.EcsactComponent {
      componentId = componentId,
      componentData = componentPtr
    };
    updates.Add(ecsComponent);
    updates_entities.Add(entityId);
	}

	public void RemoveComponent<C>(Int32 entityId)
		where     C : Ecsact.Component {
    var componentId = Ecsact.Util.GetComponentID<C>();
    var ecsComponent =
      new EcsactRuntime.EcsactComponentId { componentId = componentId };
    removes.Add(ecsComponent);
    removes_entities.Add(entityId);
	}

	public void RemoveComponent(Int32 entityId, Int32 componentId) {
		var ecsComponent =
			new EcsactRuntime.EcsactComponentId { componentId = componentId };
		removes.Add(ecsComponent);
		removes_entities.Add(entityId);
	}

	public void Alloc() {
		if(actions.Count > 0) {
			var actionsArray = actions.ToArray();

			executionOptions.actions = actionsArray;
			executionOptions.actionsLength = actionsArray.Length;
		}

		if(adds.Count > 0) {
			var addsArray = adds.ToArray();
			var addsEntitiesArray = adds_entities.ToArray();
			executionOptions.addComponents = addsArray;
			executionOptions.addComponentsLength = addsArray.Length;
			executionOptions.addComponentsEntities = addsEntitiesArray;
		}

		if(updates.Count > 0) {
			var updatesArray = updates.ToArray();
			var updatesEntitiesArray = updates_entities.ToArray();

			executionOptions.updateComponents = updatesArray;
			executionOptions.updateComponentsLength = updatesArray.Length;
			executionOptions.updateComponentsEntities = updatesEntitiesArray;
		}

		if(removes.Count > 0) {
			var removesArray = removes.ToArray();
			var removesEntitiesArray = removes_entities.ToArray();

			executionOptions.removeComponents = removesArray;
			executionOptions.removeComponentsLength = removesArray.Length;
			executionOptions.removeComponentsEntities = removesEntitiesArray;
		}
	}

	public int actionCount() {
		return actions.Count;
	}

	public void PushAction<T>(T action)
		where     T : Ecsact.Action {
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

	public void Free() {
		foreach(var ecsAction in actions) {
			Marshal.FreeHGlobal(ecsAction.actionData);
		}
		actions.Clear();

		foreach(var ecsComponent in adds) {
			Marshal.FreeHGlobal(ecsComponent.componentData);
		}
		adds.Clear();
		adds_entities.Clear();

		foreach(var ecsComponent in updates) {
			Marshal.FreeHGlobal(ecsComponent.componentData);
		}
		updates.Clear();
		updates_entities.Clear();

		removes.Clear();
		removes_entities.Clear();
	}

	public bool isEmpty() {
		if(actions.Count > 0) {
			return false;
		}
		if(adds.Count > 0) {
			return false;
		}
		if(updates.Count > 0) {
			return false;
		}
		if(removes.Count > 0) {
			return false;
		}
		return true;
	}
}

} // namespace Ecsact
