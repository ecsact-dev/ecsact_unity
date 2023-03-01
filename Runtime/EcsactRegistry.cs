using System;
using System.Collections.Generic;

using UnityEngine;

namespace Ecsact {

public class Registry {
	private EcsactRuntime rt;

	public int ID { get; private set; }

	public Registry(EcsactRuntime runtime, int regId) {
		UnityEngine.Debug.Assert(regId > -1, "Invalid registry ID");
		ID = regId;
		rt = runtime;
	}

	public void Clear() {
		rt.core.ClearRegistry(ID);
	}

	public Int32 CreateEntity() {
		return rt.core.CreateEntity(ID);
	}

	public void EnsureEntity(Int32 entityId) {
		rt.core.EnsureEntity(ID, entityId);
	}

	public bool EntityExists(Int32 entityId) {
		return rt.core.EntityExists(ID, entityId);
	}

	public void DestroyEntity(Int32 entityId) {
		rt.core.DestroyEntity(ID, entityId);
	}

	public Int32 CountEntities() {
		return rt.core.CountEntities(ID);
	}

	public void GetEntities(
		Int32     maxEntitiesCount,
		out       Int32[] outEntities,
		out Int32 outEntitiesCount
	) {
		rt.core
			.GetEntities(ID, maxEntitiesCount, out outEntities, out outEntitiesCount);
	}

	public Int32[] GetEntities() {
		return rt.core.GetEntities(ID);
	}

	internal void AddComponent<C>(int entityId, C component)
		where       C : Ecsact.Component {
    rt.core.AddComponent<C>(ID, entityId, component);
	}

	internal void AddComponent(
		Int32  entityId,
		Int32  componentId,
		object componentData
	) {
		rt.core.AddComponent(ID, entityId, componentId, componentData);
	}

	public bool HasComponent(Int32 entityId, Int32 componentId) {
		return rt.core.HasComponent(ID, entityId, componentId);
	}

	public bool HasComponent<C>(Int32 entityId)
		where     C : Ecsact.Component {
    return rt.core.HasComponent<C>(ID, entityId);
	}

	public C GetComponent<C>(Int32 entityId)
		where  C : Ecsact.Component {
    return rt.core.GetComponent<C>(ID, entityId);
	}

	public object GetComponent(Int32 entityId, Int32 componentId) {
		return rt.core.GetComponent(ID, entityId, componentId);
	}

	public Int32 CountComponents(Int32 entityId) {
		return rt.core.CountComponents(ID, entityId);
	}

	public Dictionary<Int32, object> GetComponents(Int32 entityId) {
		return rt.core.GetComponents(ID, entityId);
	}

	public void EachComponent(
		Int32                               entityId,
		EcsactRuntime.EachComponentCallback callback,
		IntPtr                              callbackUserData
	) {
		rt.core.EachComponent(ID, entityId, callback, callbackUserData);
	}

	internal void UpdateComponent(
		Int32  entityId,
		Int32  componentId,
		object componentData
	) {
		rt.core.UpdateComponent(ID, entityId, componentId, componentData);
	}

	internal void UpdateComponent<C>(Int32 entityId, C component)
		where       C : Ecsact.Component {
    rt.core.UpdateComponent<C>(ID, entityId, component);
	}

	internal void RemoveComponent<C>(Int32 entityId)
		where       C : Ecsact.Component {
    rt.core.RemoveComponent<C>(ID, entityId);
	}

	internal void RemoveComponent(Int32 entityId, Int32 componentId) {
		rt.core.RemoveComponent(ID, entityId, componentId);
	}

	public void ExecuteSystems(
		Int32 executionCount,
		EcsactRuntime.CExecutionOptions[] executionOptionsList
	) {
		rt.core.ExecuteSystems(ID, executionCount, executionOptionsList);
	}

	public void ExecuteSystems(Ecsact.ExecutionOptions executionOptions) {
		var execArr =
			new EcsactRuntime.CExecutionOptions[] { executionOptions.C() };
		rt.core.ExecuteSystems(ID, 1, execArr);
	}
}

} // namespace Ecsact
