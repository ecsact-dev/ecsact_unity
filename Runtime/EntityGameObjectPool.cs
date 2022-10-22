using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

#nullable enable

namespace Ecsact.UnitySync {

public class EntityGameObjectPool : ScriptableObject {
	public abstract class EntitySource {
		public abstract object GetComponent(Int32 entityId, Int32 componentId);
		public abstract bool   HasComponent(Int32 entityId, Int32 componentId);
	}

	public static EntityGameObjectPool CreateInstance(EntitySource entitySource) {
		var pool = (EntityGameObjectPool
		)ScriptableObject.CreateInstance(typeof(EntityGameObjectPool));

		pool._entitySource = entitySource;

		return pool;
	}

	private EntitySource? _entitySource;
	private EntitySource entitySource {
		get {
			UnityEngine.Debug.Assert(
				_entitySource != null,
				"entitySource is unset. Please use " +
					"EntityGameObjectPool.CreateInstance when creating an " +
					"EntityGameObjectPool instance.",
				this
			);
			return _entitySource!;
		}
	}

	private List<ComponentIdsList> entityComponentIds;
	private List<GameObject?>      entityGameObjects;

	private Scene? _targetScene;
	public Scene? targetScene {
		get => _targetScene;
		set {
			if(_rootGameObject != null) {
				throw new ArgumentException(
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
				throw new ArgumentException(
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

	public GameObject? GetEntityGameObject(Int32 entityId) {
		if(entityGameObjects.Count > entityId) {
			return entityGameObjects[entityId];
		}

		return null;
	}

	public static bool IsPreferredEntityGameObject(GameObject gameObject) {
		Ecsact.PreferredEntityGameObject? preferred = null;
		return gameObject.TryGetComponent(out preferred);
	}

	public void SetPreferredEntityGameObject(
		Int32      entityId,
		GameObject preferredGameObject
	) {
		var existingGameObject = GetEntityGameObject(entityId);
		if(existingGameObject != null) {
			if(IsPreferredEntityGameObject(existingGameObject)) {
				throw new global::System.Exception(
					"EntityGameObjectPool.SetPreferredEntityGameObject may not be " +
					"called on an entity that already has a preferred game object."
				);
			}

			throw new global::System.Exception(
				"TODO: Support overriding game object with preferred one"
			);
		}

		EnsureEntityLists(entityId);

		preferredGameObject.AddComponent<Ecsact.PreferredEntityGameObject>();
		entityGameObjects[entityId] = preferredGameObject;

		Scene scene = _targetScene == null ? SceneManager.GetActiveScene()
																			 : _targetScene.Value;
		if(!preferredGameObject.scene.Equals(scene)) {
			SceneManager.MoveGameObjectToScene(preferredGameObject, scene);
		}

		if(_rootGameObject != null) {
			preferredGameObject.transform.SetParent(_rootGameObject.transform);
		}
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

	public void InitComponent<T>(Int32 entityId, in T component)
		where     T : Ecsact.Component {
    InitComponent(entityId, Ecsact.Util.GetComponentID(typeof(T)), component);
	}

	public void InitComponent(
		Int32     entityId,
		Int32     componentId,
		in object component
	) {
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
				var initCompIds = UnitySyncMonoBehaviours.GetInitComponentIds(type);
				foreach(var initCompId in initCompIds) {
					if(initCompId == componentId) continue;
					if(!entitySource.HasComponent(entityId, initCompId)) continue;

					var initComponent = entitySource.GetComponent(entityId, initCompId);
					UnitySyncMonoBehaviours
						.InvokeOnInit(newMonoBehaviour, initCompId, in initComponent);
				}
			}
		}

		gameObject = gameObject ?? GetEntityGameObject(entityId);

		if(gameObject != null) {
			UnitySyncMonoBehaviours
				.InvokeOnInit(gameObject, componentId, in component);
		}
	}

	public void UpdateComponent<T>(Int32 entityId, in T component)
		where     T : Ecsact.Component {
    UpdateComponent(entityId, Ecsact.Util.GetComponentID(typeof(T)), component);
	}

	public void UpdateComponent(
		Int32     entityId,
		Int32     componentId,
		in object component
	) {
		var gameObject = GetEntityGameObject(entityId);
		if(gameObject != null) {
			UnitySyncMonoBehaviours
				.InvokeOnUpdate(gameObject, componentId, in component);
		}
	}

	public void RemoveComponent<T>(Int32 entityId, in T component)
		where     T : Ecsact.Component {
    var compObj = (object)component;
    RemoveComponent(
      entityId,
      Ecsact.Util.GetComponentID(typeof(T)),
      in compObj
    );
	}

	public void RemoveComponent(
		Int32     entityId,
		Int32     componentId,
		in object component
	) {
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

			var allMonoBehaviourTypes = UnitySyncMonoBehaviours.GetTypes(nextCompIds);

			foreach(var type in addedTypes) {
				var newMonoBehaviour = (MonoBehaviour)gameObject.AddComponent(type);
				IOnInitEntity? onInitEntity = newMonoBehaviour as IOnInitEntity;
				if(onInitEntity != null) {
					onInitEntity.OnInitEntity(entityId);
				}

				var initCompIds = UnitySyncMonoBehaviours.GetInitComponentIds(type);
				foreach(var initCompId in initCompIds) {
					if(initCompId == componentId) continue;
					if(!entitySource.HasComponent(entityId, initCompId)) continue;

					var initComponent = entitySource.GetComponent(entityId, initCompId);
					UnitySyncMonoBehaviours
						.InvokeOnInit(newMonoBehaviour, initCompId, in initComponent);
				}
			}

			UnitySyncMonoBehaviours
				.InvokeOnRemove(gameObject, componentId, in component);

			foreach(var type in removedTypes) {
				if(gameObject.TryGetComponent(type, out var removedComponent)) {
					if(Application.isPlaying) {
						Destroy(removedComponent);
					} else {
						DestroyImmediate(removedComponent);
					}
				}
			}

			if(!allMonoBehaviourTypes.Any()) {
				// Preferred entity game objects are created manually by the user. We
				// should not turn them off because they might have other behaviours
				// than the ones we put on automatically.
				if(!IsPreferredEntityGameObject(gameObject)) {
					gameObject.SetActive(false);
				}
				gameObject.name = $"entity ({entityId})";
			}
		}

		entityComponentIds[entityId].Remove(componentId);
	}

	private GameObject EnsureEntityGameObject(Int32 entityId) {
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

	private void EnsureEntityLists(Int32 entityId) {
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
		Scene scene = _targetScene == null ? SceneManager.GetActiveScene()
																			 : _targetScene.Value;

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

	private void OnChangedActiveScene(Scene current, Scene next) {
		if(_targetScene == null && _rootGameObject == null) {
			MoveEntityGameObjectsIfNeeded();
		}
	}
}

} // namespace Ecsact.UnitySync
