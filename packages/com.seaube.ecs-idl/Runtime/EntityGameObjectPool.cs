using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;

using ComponentIdsList = System.Collections.Generic.SortedSet
	< System.Int32
	>;

#nullable enable

namespace EcsIdl.UnitySync {
	public class EntityGameObjectPool : ScriptableObject {

		public static EntityGameObjectPool CreateInstance() {
			return (EntityGameObjectPool)ScriptableObject.CreateInstance(
				typeof(EntityGameObjectPool)
			);
		}

		private List<ComponentIdsList> entityComponentIds;
		private List<GameObject?> entityGameObjects;

		private Scene? _targetScene;
		public Scene? targetScene {
			get => _targetScene;
			set {
				_targetScene = value;
				MoveEntityGameObjectsIfNeeded();
			}
		}

		private EntityGameObjectPool() {
			entityComponentIds = new List<ComponentIdsList>();
			entityGameObjects = new List<GameObject?>();
		}

		void OnEnable() {
			SceneManager.activeSceneChanged += OnChangedActiveScene;
			MoveEntityGameObjectsIfNeeded();
		}

		void OnDisable() {
			SceneManager.activeSceneChanged -= OnChangedActiveScene;
		}

		public GameObject? GetEntityGameObject
			( System.Int32 entityId
			)
		{
			if(entityGameObjects.Count > entityId) {
				return entityGameObjects[entityId];
			}

			return null;
		}

		public void Clear() {
			foreach(var gameObject in entityGameObjects) {
				if(Application.isPlaying) {
					Destroy(gameObject);
				} else {
					DestroyImmediate(gameObject);
				}
			}
			entityComponentIds.Clear();
			entityGameObjects.Clear();
		}

		public void InitComponent<T>
			( System.Int32  entityId
			, T             component
			) where T : EcsIdl.Component
		{
			InitComponent(
				entityId,
				EcsIdl.Util.GetComponentID(typeof(T)),
				component
			);
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
				gameObject.SetActive(true);
				foreach(var type in addedTypes) {
					UnitySyncMonoBehaviours.InvokeOnInit(
						(MonoBehaviour)gameObject.AddComponent(type),
						componentId,
						component
					);
				}
			}
		}

		public void UpdateComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, object        component
			)
		{
			if(entityGameObjects[entityId] != null) {
				var gameObject = entityGameObjects[entityId];
				UnitySyncMonoBehaviours.InvokeOnUpdate(
					gameObject,
					entityComponentIds[entityId],
					componentId,
					component
				);
			}
		}

		public void RemoveComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, object        component
			)
		{
			if(entityGameObjects[entityId] != null) {
				var prevCompIds = entityComponentIds[entityId];
				var nextCompIds = new ComponentIdsList(entityComponentIds[entityId]);
				nextCompIds.Remove(componentId);

				var addedTypes = UnitySyncMonoBehaviours.GetAddedTypes(
					previousComponentIds: prevCompIds,
					currentComponentIds: nextCompIds
				);

				var removedTypes = UnitySyncMonoBehaviours.GetRemovedTypes(
					previousComponentIds: prevCompIds,
					currentComponentIds: nextCompIds
				);

				var gameObject = entityGameObjects[entityId]!;
				UnitySyncMonoBehaviours.InvokeOnRemove(
					gameObject,
					entityComponentIds[entityId],
					componentId,
					component
				);

				foreach(var type in removedTypes) {
					if(gameObject.TryGetComponent(type, out var removedComponent)) {
						if(Application.isPlaying) {
							Destroy(removedComponent);
						} else {
							DestroyImmediate(removedComponent);
						}
					}
				}

				foreach(var type in addedTypes) {
					UnitySyncMonoBehaviours.InvokeOnInit(
						(MonoBehaviour)gameObject.AddComponent(type),
						componentId,
						component
					);
				}

				var allMonoBehaviourTypes = UnitySyncMonoBehaviours.GetTypes(
					nextCompIds
				);
				if(!allMonoBehaviourTypes.Any()) {
					gameObject.SetActive(false);
					gameObject.name = $"entity ({entityId})";
				}
			}

			entityComponentIds[entityId].Remove(componentId);
		}

		private GameObject EnsureEntityGameObject
			( System.Int32 entityId
			)
		{
			GameObject? gameObject = entityGameObjects[entityId];
			if(gameObject == null) {
				gameObject = new GameObject($"entity ({entityId})");
				if(_targetScene != null && !gameObject.scene.Equals(_targetScene)) {
					SceneManager.MoveGameObjectToScene(gameObject, _targetScene.Value);
				}
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

		private void MoveEntityGameObjectsIfNeeded() {
			Scene scene = _targetScene == null
				? SceneManager.GetActiveScene()
				: _targetScene.Value	;

			foreach(var gameObject in entityGameObjects) {
				if(gameObject != null) {
					// Game objects should have been created in their correct scenes. If
					// we have _any_ that equal our target it is assumed they all are.
					// This return early is an optimization.
					if(gameObject.scene.Equals(scene)) {
						return;
					}
				}
			}

			foreach(var gameObject in entityGameObjects) {
				if(gameObject != null) {
					SceneManager.MoveGameObjectToScene(gameObject, scene);
				}
			}
		}

    private void OnChangedActiveScene
			( Scene current
			, Scene next
			)
		{
			if(_targetScene == null) {
				MoveEntityGameObjectsIfNeeded();
			}
		}
	}
}
