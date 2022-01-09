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
				if(_rootGameObject != null) {
					throw new System.ArgumentException(
						"EntityGameObjectPool.targetScene may not be set if " +
						"EntityGameObjectPool.rootGameObject is set."
					);
				}

				if(_targetScene != null && value != null) {
					if(_targetScene.Equals(value)) {
						return;
					}
				}

				_targetScene = value;
				MoveEntityGameObjectsIfNeeded();
			}
		}

		private GameObject? _rootGameObject;
		public GameObject? rootGameObject {
			get => _rootGameObject;
			set {
				if(_targetScene != null) {
					throw new System.ArgumentException(
						"EntityGameObjectPool.rootGameObject may not be set if " +
						"EntityGameObjectPool.targetScene is set."
					);
				}

				if(_rootGameObject != null && value != null) {
					if(GameObject.ReferenceEquals(_rootGameObject, value)) {
						return;
					}
				}

				_rootGameObject = value;
				ReparentEntityGameObjects();
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
			, in T          component
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
			, in object     component
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

			GameObject? gameObject = null;
			if(removedTypes.Any() && entityGameObjects[entityId] != null) {
				gameObject = entityGameObjects[entityId]!;
				foreach(var type in removedTypes) {
					if(gameObject.TryGetComponent(type, out var removedComponent)) {
						UnityEngine.Object.Destroy(removedComponent);
					}
				}
			}

			if(addedTypes.Any()) {
				gameObject = EnsureEntityGameObject(entityId);
				gameObject.SetActive(true);
				foreach(var type in addedTypes) {
					var newMonoBehaviour = (MonoBehaviour)gameObject.AddComponent(type);
					IOnInitEntity? onInitEntity = newMonoBehaviour as IOnInitEntity;
					if(onInitEntity != null) {
						onInitEntity.OnInitEntity(entityId);
					}
				}
			}

			gameObject = gameObject ?? GetEntityGameObject(entityId);
			
			if(gameObject != null) {
				UnitySyncMonoBehaviours.InvokeOnInit(
					gameObject,
					componentId,
					in component
				);
			}
		}

		public void UpdateComponent<T>
			( System.Int32  entityId
			, in T          component
			) where T : EcsIdl.Component
		{
			UpdateComponent(
				entityId,
				EcsIdl.Util.GetComponentID(typeof(T)),
				component
			);
		}

		public void UpdateComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, in object     component
			)
		{
			var gameObject = GetEntityGameObject(entityId);
			if(gameObject == null) {
				throw new System.ArgumentException(
					$"EntityGameObjectPool.UpdateComponent called before " +
					$"EntityGameObjectPool.InitComponent. entityId={entityId}"
				);
			}

			UnitySyncMonoBehaviours.InvokeOnUpdate(
				gameObject,
				componentId,
				in component
			);
		}

		public void RemoveComponent<T>
			( System.Int32  entityId
			, in T          component
			) where T : EcsIdl.Component
		{
			var compObj = (object)component;
			RemoveComponent(
				entityId,
				EcsIdl.Util.GetComponentID(typeof(T)),
				in compObj
			);
		}

		public void RemoveComponent
			( System.Int32  entityId
			, System.Int32  componentId
			, in object      component
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
					componentId,
					in component
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
					var newMonoBehaviour = (MonoBehaviour)gameObject.AddComponent(type);
					IOnInitEntity? onInitEntity = newMonoBehaviour as IOnInitEntity;
					if(onInitEntity != null) {
						onInitEntity.OnInitEntity(entityId);
					}
					UnitySyncMonoBehaviours.InvokeOnInit(
						newMonoBehaviour,
						componentId,
						in component
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
				} else if(_rootGameObject != null) {
					gameObject.transform.SetParent(_rootGameObject.transform);
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

		private void ReparentEntityGameObjects() {
			foreach(var gameObject in entityGameObjects) {
				if(gameObject != null) {
					if(_rootGameObject == null) {
						gameObject.transform.SetParent(null);
					} else {
						gameObject.transform.SetParent(_rootGameObject.transform);
					}
				}
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
			if(_targetScene == null && _rootGameObject == null) {
				MoveEntityGameObjectsIfNeeded();
			}
		}
	}
}
