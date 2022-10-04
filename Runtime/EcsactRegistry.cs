using System;
using System.Collections.Generic;

using UnityEngine;

namespace Ecsact {
	public class Registry : MonoBehaviour {
		
		private EcsactRuntime rt;
		private int registryId;

		public Registry
			( EcsactRuntime runtime
			, int regId
			) 
		{
			rt = runtime;
			registryId = regId;
		}

		public Registry
			( EcsactRuntime runtime
			, string registryName
			)
		{
			rt = runtime;
			registryId = rt.core.CreateRegistry(registryName);
		}

		public Int32 CreateEntity() {
			return rt.core.CreateEntity(registryId);
		}

		public void EnsureEntity
			( Int32 entityId
			)
		{
			rt.core.EnsureEntity(registryId, entityId);
		}

		public bool EntityExists
			( Int32 entityId
			)
		{
			return rt.core.EntityExists(registryId, entityId);
		}

		public void DestroyEntity
			( Int32 entityId
			)
		{
			rt.core.DestroyEntity(registryId, entityId);
		}

		public Int32 CountEntities() {
			return rt.core.CountEntities(registryId);
		}

		public void GetEntities
			( Int32        maxEntitiesCount
			, out Int32[]  outEntities
			, out Int32    outEntitiesCount
			)
		{
			rt.core.GetEntities(
				registryId,
				maxEntitiesCount,
				out outEntities,
				out outEntitiesCount
			);
		}

		public Int32[] GetEntities() {
			return rt.core.GetEntities(registryId);
		}

		public void AddComponent<C>
			( int entityId
			, C component
			) where C : Ecsact.Component
		{
			rt.core.AddComponent<C>(
				registryId,
				entityId,
				component
			);
		}

		public void AddComponent
			( Int32   entityId
			, Int32   componentId
			, object  componentData
			)
		{
			rt.core.AddComponent(registryId, entityId, componentId, componentData);
		}

		public bool HasComponent
			( Int32  entityId
			, Int32  componentId
			)
		{
			return rt.core.HasComponent(registryId, entityId, componentId);
		}

		public bool HasComponent<C>
			( Int32 entityId
			) where C : Ecsact.Component
		{
			return rt.core.HasComponent<C>(registryId, entityId);
		}

		public C GetComponent<C>
			( Int32 entityId
			) where C : Ecsact.Component
		{
			return rt.core.GetComponent<C>(registryId, entityId);
		}

		public object GetComponent
			( Int32 entityId
			, Int32 componentId
			)
		{
			return rt.core.GetComponent(registryId, entityId, componentId);
		}

		public Int32 CountComponents
			( Int32 entityId
			)
		{
			return rt.core.CountComponents(registryId, entityId); 
		}

		public Dictionary<Int32, object> GetComponents
			( Int32  entityId
			)
		{
			return rt.core.GetComponents(registryId, entityId);
		}

		public void EachComponent
			( Int32                                entityId
			, EcsactRuntime.EachComponentCallback  callback
			, IntPtr                               callbackUserData
			)
		{
			rt.core.EachComponent(registryId, entityId, callback, callbackUserData);
		}

		public void UpdateComponent<C>
			( Int32  entityId
			, C      component
			) where C : Ecsact.Component
		{
			rt.core.UpdateComponent<C>(registryId, entityId, component);
		}

		public void RemoveComponent<C>
			( Int32  entityId
			) where C : Ecsact.Component
		{
			rt.core.RemoveComponent<C>(registryId, entityId);
		}

		public void RemoveComponent
			( Int32  entityId
			, Int32  componentId
			)
		{
			rt.core.RemoveComponent(registryId, entityId, componentId);
		}

		public void ExecuteSystems
			( Int32                             executionCount
			, EcsactRuntime.ExecutionOptions[]  executionOptionsList
			)
		{
			rt.core.ExecuteSystems(registryId, executionCount, executionOptionsList);
		}
	}
}
