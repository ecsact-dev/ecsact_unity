using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using ComponentIdsList = System.Collections.Generic.SortedSet
	< System.Int32
	>;

#nullable enable

namespace EcsIdl.UnitySync {
	public class EntityGameObjectPool {
		private List<ComponentIdsList> entityComponentIds;
		private List<GameObject?> entityGameObjects;

		public EntityGameObjectPool() {
			entityComponentIds = new List<ComponentIdsList>();
			entityGameObjects = new List<GameObject?>();
		}

		public void InitComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, object        component
			)
		{
			EnsureEntityLists(entityId);
			var compIds = entityComponentIds[entityId];
			var prevCompIds = new ComponentIdsList(compIds);
			compIds.Add(componentId);

			var addedTypes = UnitySyncMonoBehaviours.GetAddedTypes(
				previousComponentIds: prevCompIds,
				currentComponentIds: compIds
			);

			var removedTypes = UnitySyncMonoBehaviours.GetRemovedTypes(
				previousComponentIds: prevCompIds,
				currentComponentIds: compIds
			);

			if(removedTypes.Any() && entityGameObjects[entityId] != null) {
				var gameObject = entityGameObjects[entityId]!;
				foreach(var type in removedTypes) {
					if(gameObject.TryGetComponent(type, out var removedComponent)) {
						UnityEngine.Object.Destroy(removedComponent);
					}
				}
			}

			if(addedTypes.Any()) {
				var gameObject = EnsureEntityGameObject(entityId);
				foreach(var type in addedTypes) {
					var addedComponent = gameObject.AddComponent(type);
				}
			}
		}

		public void UpdateComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, object        component
			)
		{

		}

		public void RemoveComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, object        component
			)
		{
			entityComponentIds[entityId].Remove(componentId);
		}

		private GameObject EnsureEntityGameObject
			( System.Int32 entityId
			)
		{
			GameObject? gameObject = entityGameObjects[entityId];
			if(gameObject == null) {
				gameObject = new GameObject($"entity ({entityId})");
				entityGameObjects[entityId] = gameObject;
			}

			return gameObject;
		}

		private void EnsureEntityLists
			( System.Int32 entityId
			)
		{
			var capacity = Math.Max(entityId + 1, entityComponentIds.Count);
			entityGameObjects.Capacity = capacity;
			entityComponentIds.Capacity = capacity;

			while(entityGameObjects.Count < capacity) {
				entityGameObjects.Add(null);
			}

			while(entityComponentIds.Count < capacity) {
				entityComponentIds.Add(new ComponentIdsList());
			}
		}
	}
}
