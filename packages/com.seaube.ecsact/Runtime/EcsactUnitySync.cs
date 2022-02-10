using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using TypeComponentIdsMap = System.Collections.Generic.Dictionary
	< System.Type
	, System.Collections.Generic.SortedSet<System.Int32>
	>;

using ComponentIdBehavioursMap = System.Collections.Generic.Dictionary
	< System.Int32
	, System.Collections.Generic.HashSet<System.Type>
	>;

using ComponentIdsList = System.Collections.Generic.SortedSet
	< System.Int32
	>;

namespace Ecsact.UnitySync {

	/// <summary> Required marker interface. If a MonoBehaviour implements this 
	/// marker it will be added to the game object if the entity has component 
	/// <c>T</c>. All required marker interfaces must resolve before the 
	/// behaviour is added.</summary>
	public interface IRequired<T> where T : Ecsact.Component {}

	public interface IOnInitEntity {
		void OnInitEntity(System.Int32 entityId);
	}

	public interface IOnInitComponent<T> where T : Ecsact.Component {
		void OnInitComponent(in T component);
	}

	public interface IOnUpdateComponent<T> where T : Ecsact.Component {
		void OnUpdateComponent(in T component);
	}

	public interface IOnRemoveComponent<T> where T : Ecsact.Component {
		void OnRemoveComponent(in T component);
	}

	public static class UnitySyncMonoBehaviours {
		private static ComponentIdsList knownComponentIds;
		private static Dictionary<Int32, Type> knownComponentTypes;

		private static TypeComponentIdsMap requiredComponentsMap;
		private static TypeComponentIdsMap onInitComponentsMap;
		private static TypeComponentIdsMap onUpdateComponentsMap;
		private static TypeComponentIdsMap onRemoveComponentsMap;

		private static ComponentIdBehavioursMap requiredBehavioursMap;
		private static ComponentIdBehavioursMap onInitBehavioursMap;
		private static ComponentIdBehavioursMap onUpdateBehavioursMap;
		private static ComponentIdBehavioursMap onRemoveBehavioursMap;

		static UnitySyncMonoBehaviours() {
			knownComponentIds = new ComponentIdsList();
			knownComponentTypes = new Dictionary<Int32, Type>();

			requiredComponentsMap = new TypeComponentIdsMap();
			onInitComponentsMap = new TypeComponentIdsMap();
			onUpdateComponentsMap = new TypeComponentIdsMap();
			onRemoveComponentsMap = new TypeComponentIdsMap();

			requiredBehavioursMap = new ComponentIdBehavioursMap();
			onInitBehavioursMap = new ComponentIdBehavioursMap();
			onUpdateBehavioursMap = new ComponentIdBehavioursMap();
			onRemoveBehavioursMap = new ComponentIdBehavioursMap();
		}

		public static void InvokeOnInit
			( GameObject  gameObject
			, Int32       componentId
			, in object   component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			if(onInitBehavioursMap.TryGetValue(componentId, out var s)) {
				foreach(var monoBehaviourType in s) {
					if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
						InvokeOnInit((MonoBehaviour)mb, componentId, in component);
					}
				}
			}
		}

		public static void InvokeOnInit
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, in object      component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;
			var componentType = knownComponentTypes[componentId];

			foreach(var i in monoBehaviour.GetType().GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();
				if(genericTypeDef == typeof(IOnInitComponent<>)) {
					var targetComponentType = i.GetGenericArguments()[0];
					if(targetComponentType.Equals(componentType)) {
						i.GetMethod("OnInitComponent").Invoke(
							monoBehaviour,
							new object[]{component}
						);
					}
				}
			}
		}

		public static void InvokeOnUpdate
			( GameObject  gameObject
			, Int32       componentId
			, in object   component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			if(onUpdateBehavioursMap.TryGetValue(componentId, out var s)) {
				foreach(var monoBehaviourType in s) {
					if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
						InvokeOnUpdate((MonoBehaviour)mb, componentId, in component);
					}
				}
			}
		}

		public static void InvokeOnUpdate
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, in object      component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;
			var componentType = knownComponentTypes[componentId];

			foreach(var i in monoBehaviour.GetType().GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();
				if(genericTypeDef == typeof(IOnUpdateComponent<>)) {
					var targetComponentType = i.GetGenericArguments()[0];
					if(targetComponentType.Equals(componentType)) {
						i.GetMethod("OnUpdateComponent").Invoke(
							monoBehaviour,
							new object[]{component}
						);
					}
				}
			}
		}

		public static void InvokeOnRemove
			( GameObject  gameObject
			, Int32       componentId
			, in object   component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			if(onRemoveBehavioursMap.TryGetValue(componentId, out var s)) {
				foreach(var monoBehaviourType in s) {
					if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
						InvokeOnRemove((MonoBehaviour)mb, componentId, in component);
					}
				}
			}
		}

		public static void InvokeOnRemove
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, in object      component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;
			var componentType = knownComponentTypes[componentId];

			foreach(var i in monoBehaviour.GetType().GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();
				if(genericTypeDef == typeof(IOnRemoveComponent<>)) {
					var targetComponentType = i.GetGenericArguments()[0];
					if(targetComponentType.Equals(componentType)) {
						i.GetMethod("OnRemoveComponent").Invoke(
							monoBehaviour,
							new object[]{component}
						);
					}
				}
			}
		}

		/// <summary>Get all <c>MonoBehaviour</c> types that should be added to an
		/// Entity <c>GameObject</c> when it has <c>componentIds</c>.</summary>
		public static IEnumerable<Type> GetTypes
			( ComponentIdsList componentIds
			)
		{
			var compIds = new ComponentIdsList(
				knownComponentIds.Intersect(componentIds)
			);

			var usedTypes = new HashSet<Type>();

			foreach(var compId in compIds) {
				if(requiredBehavioursMap.TryGetValue(compId, out var types)) {
					foreach(var type in types) {
						if(usedTypes.Contains(type)) continue;

						if(requiredComponentsMap.TryGetValue(type, out var reqCompIds)) {
							bool missingRequired = false;
							foreach(var reqCompId in reqCompIds) {
								if(!compIds.Contains(reqCompId)) {
									missingRequired = true;
									break;
								}
							}

							if(missingRequired) {
								continue;
							}
						}

						yield return type;
						usedTypes.Add(type);
					}
				}

				if(onInitBehavioursMap.TryGetValue(compId, out types)) {
					foreach(var type in types) {
						if(usedTypes.Contains(type)) continue;

						if(requiredComponentsMap.TryGetValue(type, out var reqCompIds)) {
							bool missingRequired = false;
							foreach(var reqCompId in reqCompIds) {
								if(!compIds.Contains(reqCompId)) {
									missingRequired = true;
									break;
								}
							}

							if(missingRequired) {
								continue;
							}
						}

						yield return type;
						usedTypes.Add(type);
					}
				}

				if(onUpdateBehavioursMap.TryGetValue(compId, out types)) {
					foreach(var type in types) {
						if(usedTypes.Contains(type)) continue;

						if(requiredComponentsMap.TryGetValue(type, out var reqCompIds)) {
							bool missingRequired = false;
							foreach(var reqCompId in reqCompIds) {
								if(!compIds.Contains(reqCompId)) {
									missingRequired = true;
									break;
								}
							}

							if(missingRequired) {
								continue;
							}
						}

						yield return type;
						usedTypes.Add(type);
					}
				}

				if(onRemoveBehavioursMap.TryGetValue(compId, out types)) {
					foreach(var type in types) {
						if(usedTypes.Contains(type)) continue;

						if(requiredComponentsMap.TryGetValue(type, out var reqCompIds)) {
							bool missingRequired = false;
							foreach(var reqCompId in reqCompIds) {
								if(!compIds.Contains(reqCompId)) {
									missingRequired = true;
									break;
								}
							}

							if(missingRequired) {
								continue;
							}
						}

						yield return type;
						usedTypes.Add(type);
					}
				}
			}
		}

		public static IEnumerable<Type> GetAddedTypes
			( ComponentIdsList previousComponentIds
			, ComponentIdsList currentComponentIds
			)
		{
			var previousTypes = GetTypes(previousComponentIds);
			var currentTypes = GetTypes(currentComponentIds);

			return currentTypes.Except(previousTypes);
		}

		public static IEnumerable<Type> GetRemovedTypes
			( ComponentIdsList previousComponentIds
			, ComponentIdsList currentComponentIds
			)
		{
			var previousTypes = GetTypes(previousComponentIds);
			var currentTypes = GetTypes(currentComponentIds);

			return previousTypes.Except(currentTypes);
		}

		public static bool HasInterfaces
			( Type type
			)
		{
			foreach(var i in type.GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();

				if(genericTypeDef == typeof(IRequired<>)) {
					return true;
				} else if(genericTypeDef == typeof(IOnInitComponent<>)) {
					return true;
				} else if(genericTypeDef == typeof(IOnUpdateComponent<>)) {
					return true;
				} else if(genericTypeDef == typeof(IOnRemoveComponent<>)) {
					return true;
				}
			}

			return false;
		}

		public static IEnumerable<Type> GetInterfaces
			( Type type
			)
		{
			foreach(var i in type.GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();

				if(genericTypeDef == typeof(IRequired<>)) {
					yield return i;
				} else if(genericTypeDef == typeof(IOnInitComponent<>)) {
					yield return i;
				} else if(genericTypeDef == typeof(IOnUpdateComponent<>)) {
					yield return i;
				} else if(genericTypeDef == typeof(IOnRemoveComponent<>)) {
					yield return i;
				}
			}
		}

		public static IEnumerable<System.Int32> GetInitComponentIds
			( System.Type type
			)
		{
			if(onInitComponentsMap.TryGetValue(type, out var compIds)) {
				foreach(var compId in compIds) {
					yield return compId;
				}
			}
		}

		public static IEnumerable<System.Int32> GetRemoveComponentIds
			( System.Type type
			)
		{
			if(onInitComponentsMap.TryGetValue(type, out var compIds)) {
				foreach(var compId in compIds) {
					yield return compId;
				}
			}
		}

		public static IEnumerable<System.Int32> GetRequiredComponentIds
			( System.Type type
			)
		{
			if(requiredComponentsMap.TryGetValue(type, out var compIds)) {
				foreach(var compId in compIds) {
					yield return compId;
				}
			}
		}

		public static void ClearRegisteredMonoBehaviourTypes() {
			knownComponentIds.Clear();
			knownComponentTypes.Clear();

			requiredComponentsMap.Clear();
			onInitComponentsMap.Clear();
			onUpdateComponentsMap.Clear();
			onRemoveComponentsMap.Clear();

			requiredBehavioursMap.Clear();
			onInitBehavioursMap.Clear();
			onUpdateBehavioursMap.Clear();
			onRemoveBehavioursMap.Clear();
		}

		public static IEnumerable<Type> RegisterMonoBehaviourTypes
			( IEnumerable<Type> types
			)
		{
			var registeredTypes = new List<Type>();
			foreach(var type in types) {
				if(RegisterMonoBehaviourInterfaces(type)) {
					registeredTypes.Add(type);
				}
			}

			foreach(var type in registeredTypes) {
				EndMonoBehaviourRegistration(type);
				yield return type;
			}
		}

		public static bool RegisterMonoBehaviourType<T>()
			where T : MonoBehaviour
		{
			return RegisterMonoBehaviourType(typeof(T));
		}

		public static bool RegisterMonoBehaviourType
			( Type type
			)
		{
			if(RegisterMonoBehaviourInterfaces(type)) {
				EndMonoBehaviourRegistration(type);
				return true;
			}

			return false;
		}

		private static bool RegisterMonoBehaviourInterfaces
			( Type type
			)
		{
			var registrationBegan = false;
			void beginRegistrationIfNeeded() {
				if(!registrationBegan) {
					registrationBegan = true;
					BeginMonoBehaviourRegistration(type);
				}
			}

			foreach(var i in type.GetInterfaces()) {
				// All unity sync interfaces are generic. Skip.
				if(!i.IsGenericType) continue;

				var genericTypeDef = i.GetGenericTypeDefinition();

				if(genericTypeDef == typeof(IRequired<>)) {
					beginRegistrationIfNeeded();
					RegisterRequiredInterface(type, i.GetGenericArguments()[0]);
				} else if(genericTypeDef == typeof(IOnInitComponent<>)) {
					beginRegistrationIfNeeded();
					RegisterOnInitComponentInterface(type, i.GetGenericArguments()[0]);
				} else if(genericTypeDef == typeof(IOnUpdateComponent<>)) {
					beginRegistrationIfNeeded();
					RegisterOnUpdateComponentInterface(type, i.GetGenericArguments()[0]);
				} else if(genericTypeDef == typeof(IOnRemoveComponent<>)) {
					beginRegistrationIfNeeded();
					RegisterOnRemoveComponentInterface(type, i.GetGenericArguments()[0]);
				}
			}

			return registrationBegan;
		}

		private static void BeginMonoBehaviourRegistration
			( Type monoBehaviourType
			)
		{
			requiredComponentsMap[monoBehaviourType] = new ComponentIdsList();
			onInitComponentsMap[monoBehaviourType] = new ComponentIdsList();
			onUpdateComponentsMap[monoBehaviourType] = new ComponentIdsList();
			onRemoveComponentsMap[monoBehaviourType] = new ComponentIdsList();
		}

		private static void EndMonoBehaviourRegistration
			( Type monoBehaviourType
			)
		{
		}

		private static void RegisterRequiredInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = Ecsact.Util.GetComponentID(componentType);
			requiredComponentsMap[monoBehaviourType].Add(componentId);
			GetRequiredBehaviours(componentId).Add(monoBehaviourType);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnInitComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = Ecsact.Util.GetComponentID(componentType);
			onInitComponentsMap[monoBehaviourType].Add(componentId);
			GetOnInitBehaviours(componentId).Add(monoBehaviourType);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnUpdateComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = Ecsact.Util.GetComponentID(componentType);
			onUpdateComponentsMap[monoBehaviourType].Add(componentId);
			GetOnUpdateBehaviours(componentId).Add(monoBehaviourType);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnRemoveComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = Ecsact.Util.GetComponentID(componentType);
			onRemoveComponentsMap[monoBehaviourType].Add(componentId);
			GetOnRemoveBehaviours(componentId).Add(monoBehaviourType);
			AddKnownComponentId(componentId);
		}


		private static void AddKnownComponentId
			( System.Int32 componentId
			)
		{
			if(knownComponentIds.Add(componentId)) {
				// New known component
				knownComponentTypes.Add(
					componentId,
					Ecsact.Util.GetComponentType(componentId)!
				);
			}
		}

		private static HashSet<Type> GetRequiredBehaviours
			( Int32 componentId
			)
		{
			HashSet<Type> behaviours;
			if(!requiredBehavioursMap.TryGetValue(componentId, out behaviours)) {
				behaviours = new HashSet<Type>();
				requiredBehavioursMap[componentId] = behaviours;
			}
			return behaviours;
		}

		private static HashSet<Type> GetOnInitBehaviours
			( Int32 componentId
			)
		{
			HashSet<Type> behaviours;
			if(!onInitBehavioursMap.TryGetValue(componentId, out behaviours)) {
				behaviours = new HashSet<Type>();
				onInitBehavioursMap[componentId] = behaviours;
			}
			return behaviours;
		}

		private static HashSet<Type> GetOnUpdateBehaviours
			( Int32 componentId
			)
		{
			HashSet<Type> behaviours;
			if(!onUpdateBehavioursMap.TryGetValue(componentId, out behaviours)) {
				behaviours = new HashSet<Type>();
				onUpdateBehavioursMap[componentId] = behaviours;
			}
			return behaviours;
		}

		private static HashSet<Type> GetOnRemoveBehaviours
			( Int32 componentId
			)
		{
			HashSet<Type> behaviours;
			if(!onRemoveBehavioursMap.TryGetValue(componentId, out behaviours)) {
				behaviours = new HashSet<Type>();
				onRemoveBehavioursMap[componentId] = behaviours;
			}
			return behaviours;
		}
	}
}
