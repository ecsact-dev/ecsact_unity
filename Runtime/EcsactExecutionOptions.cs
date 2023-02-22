using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace Ecsact {

public class ExecutionOptions {
	public class BuilderEntity {
		public BuilderEntity AddComponent<C>(C component)
			where              C : Ecsact.Component {
      var componentId = Ecsact.Util.GetComponentID<C>();
      components.Add(componentId, component);
      return this;
		}
		public EcsactRuntime.EntityIdCallback callback;
		internal Dictionary<Int32, object> components = new();
	};

	public EcsactRuntime.CExecutionOptions executionOptions;

	private List<EcsactRuntime.EcsactAction>      actions;
	private List<EcsactRuntime.EcsactComponent>   adds;
	private List<Int32>                           adds_entities;
	private List<EcsactRuntime.EcsactComponent>   updates;
	private List<Int32>                           updates_entities;
	private List<EcsactRuntime.EcsactComponentId> removes;
	private List<Int32>                           removes_entities;

	public List<BuilderEntity>                        create_entities;
	private List<List<EcsactRuntime.EcsactComponent>> create_entities_components;
	public List<Int32>     create_entities_placeholders;
	private List<Int32>    create_entities_components_length;
	private List<GCHandle> create_entity_pins;

	private List<EcsactRuntime.EcsactComponentId> destroy_entities;

	internal ExecutionOptions() {
		actions = new();
		adds = new();
		adds_entities = new();
		updates = new();
		updates_entities = new();
		removes = new();
		removes_entities = new();
		executionOptions = new();
		create_entities = new();
		create_entities_components = new();
		create_entities_placeholders = new();
		create_entities_components_length = new();
		create_entity_pins = new();
		destroy_entities = new();
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

	public BuilderEntity CreateEntity(EcsactRuntime.EntityIdCallback callback) {
		BuilderEntity builder = new();
		builder.callback = callback;

		create_entities.Add(builder);
		return builder;
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

		if(create_entities.Count > 0) {
			executionOptions.createEntitiesLength = create_entities.Count;
			create_entities_placeholders = new List<Int32>(create_entities.Count);
			executionOptions.createEntitiesComponentsLength =
				new Int32[create_entities.Count];
			executionOptions.createEntitiesComponents =
				new IntPtr[create_entities.Count];

			create_entities_components =
				new List<List<EcsactRuntime.EcsactComponent>>(create_entities.Count);

			for(int i = 0; i < create_entities.Count; i++) {
				var builder = create_entities[i];

				executionOptions.createEntitiesComponentsLength[i] =
					builder.components.Count;

				// Lists can't be intptrs
				var compList = new List<EcsactRuntime.EcsactComponent>();
				create_entities_components.Add(compList);

				foreach(var component in builder.components) {
					var componentPtr =
						Marshal.AllocHGlobal(Marshal.SizeOf(component.Value));

					Ecsact.Util
						.ComponentToPtr(component.Value, component.Key, componentPtr);

					EcsactRuntime.EcsactComponent ecsactComponent;
					ecsactComponent.componentData = componentPtr;
					ecsactComponent.componentId = component.Key;

					compList.Add(ecsactComponent);
				}

				if(builder.components.Count > 0) {
					var createPinned =
						GCHandle.Alloc(compList.ToArray(), GCHandleType.Pinned);

					create_entity_pins.Add(createPinned);

					var createEntityComponentsPinned = createPinned.AddrOfPinnedObject();

					executionOptions.createEntitiesComponents[i] =
						createEntityComponentsPinned;
				} else {
					executionOptions.createEntitiesComponents[i] = IntPtr.Zero;
				}
			}
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

		create_entities_placeholders.Clear();

		foreach(var componentList in create_entities_components) {
			foreach(var ecsComponent in componentList) {
				Marshal.FreeHGlobal(ecsComponent.componentData);
			}
		}
		create_entities_components.Clear();

		foreach(var pin in create_entity_pins) {
			pin.Free();
		}

		create_entities_components_length.Clear();
		create_entity_pins.Clear();
		create_entities.Clear();

		destroy_entities.Clear();
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
		if(create_entities.Count > 0) {
			return false;
		}
		if(destroy_entities.Count > 0) {
			return false;
		}
		return true;
	}
}

} // namespace Ecsact
