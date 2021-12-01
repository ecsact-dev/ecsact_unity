using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using TypeComponentIdsMap = System.Collections.Generic.Dictionary
	< System.Type
	, System.Collections.Generic.SortedSet<System.Int32>
	>;

using ComponentIdsList = System.Collections.Generic.SortedSet
	< System.Int32
	>;

using ComponentIdsTypesMap = System.Collections.Generic.Dictionary
	< System.Collections.Generic.SortedSet<System.Int32>
	, System.Collections.Generic.HashSet<System.Type>
	>;

namespace EcsIdl.UnitySync {

	/// <summary> Required marker interface. If a MonoBehaviour implements this 
	/// marker it will be added to the game object if the entity has component 
	/// <c>T</c>. All required marker interfaces must resolve before the 
	/// behaviour is added.</summary>
	public interface IRequired<T> where T : EcsIdl.Component {}

	public interface IOnInitComponent<T> where T : EcsIdl.Component {
		void OnInitComponent(in T component);
	}

	public interface IOnUpdateComponent<T> where T : EcsIdl.Component {
		void OnUpdateComponent(in T component);
	}

	public interface IOnRemoveComponent<T> where T : EcsIdl.Component {
		void OnRemoveComponent(in T component);
	}

	public static class UnitySyncMonoBehaviours {
		private static ComponentIdsList knownComponentIds;
		private static Dictionary<Int32, Type> knownComponentTypes;
		private static TypeComponentIdsMap requiredComponentsMap;
		private static TypeComponentIdsMap onInitComponentsMap;
		private static TypeComponentIdsMap onUpdateComponentsMap;
		private static TypeComponentIdsMap onRemoveComponentsMap;

		private static ComponentIdsTypesMap monoBehaviourTypes;

		static UnitySyncMonoBehaviours() {
			knownComponentIds = new ComponentIdsList();
			knownComponentTypes = new Dictionary<Int32, Type>();
			requiredComponentsMap = new TypeComponentIdsMap();
			onInitComponentsMap = new TypeComponentIdsMap();
			onUpdateComponentsMap = new TypeComponentIdsMap();
			onRemoveComponentsMap = new TypeComponentIdsMap();
			monoBehaviourTypes = new ComponentIdsTypesMap(
				ComponentIdsList.CreateSetComparer()
			);
		}

		public static void InvokeOnInit
			( GameObject        gameObject
			, ComponentIdsList  componentIds
			, Int32             componentId
			, object            component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			foreach(var monoBehaviourType in monoBehaviourTypes[componentIds]) {
				if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
					InvokeOnInit((MonoBehaviour)mb, componentId, component);
				}
			}
		}

		public static void InvokeOnInit
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, object         component
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

		public static void InvokeOnInit<T>
			( T       monoBehaviour
			, Int32   componentId
			, object  component
			) where T : MonoBehaviour
		{
			InvokeOnInit(monoBehaviour, componentId, component);
		}

		public static void InvokeOnUpdate
			( GameObject        gameObject
			, ComponentIdsList  componentIds
			, Int32             componentId
			, object            component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			componentIds = new ComponentIdsList(
				knownComponentIds.Intersect(componentIds)
			);

			foreach(var monoBehaviourType in monoBehaviourTypes[componentIds]) {
				if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
					InvokeOnUpdate((MonoBehaviour)mb, componentId, component);
				}
			}
		}

		public static void InvokeOnUpdate
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, object         component
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

		public static void InvokeOnUpdate<T>
			( T       monoBehaviour
			, Int32   componentId
			, object  component
			) where T : MonoBehaviour
		{
			InvokeOnUpdate(monoBehaviour, componentId, component);
		}

		public static void InvokeOnRemove
			( GameObject        gameObject
			, ComponentIdsList  componentIds
			, Int32             componentId
			, object            component
			)
		{
			if(!knownComponentIds.Contains(componentId)) return;

			componentIds = new ComponentIdsList(
				knownComponentIds.Intersect(componentIds)
			);

			foreach(var monoBehaviourType in monoBehaviourTypes[componentIds]) {
				if(gameObject.TryGetComponent(monoBehaviourType, out var mb)) {
					InvokeOnRemove((MonoBehaviour)mb, componentId, component);
				}
			}
		}

		public static void InvokeOnRemove
			( MonoBehaviour  monoBehaviour
			, Int32          componentId
			, object         component
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

		public static void InvokeOnRemove<T>
			( T       monoBehaviour
			, Int32   componentId
			, object  component
			) where T : MonoBehaviour
		{
			InvokeOnRemove(monoBehaviour, componentId, component);
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

			if(monoBehaviourTypes.TryGetValue(compIds, out var types)) {
				foreach(var type in types) {
					yield return type;
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

		public static void ClearRegisteredMonoBehaviourTypes() {
			knownComponentIds.Clear();
			knownComponentTypes.Clear();
			requiredComponentsMap.Clear();
			onInitComponentsMap.Clear();
			onUpdateComponentsMap.Clear();
			onRemoveComponentsMap.Clear();
			monoBehaviourTypes.Clear();

			// Always have the 'no components' list
			monoBehaviourTypes[new ComponentIdsList()] = new HashSet<System.Type>();
		}

		public static void RegisterMonoBehaviourType<T>() where T : MonoBehaviour {
			RegisterMonoBehaviourType(typeof(T));
		}

		public static void RegisterMonoBehaviourType
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

			if(registrationBegan) {
				EndMonoBehaviourRegistration(type);
			}
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
			var compIds = new ComponentIdsList();
			var reqCompIds = new ComponentIdsList();

			reqCompIds.UnionWith(requiredComponentsMap[monoBehaviourType]);
			compIds.UnionWith(onInitComponentsMap[monoBehaviourType]);
			compIds.UnionWith(onUpdateComponentsMap[monoBehaviourType]);
			compIds.UnionWith(onRemoveComponentsMap[monoBehaviourType]);

			var knownComponentIdsPermutations =
				EcsIdl.Util.GetComponentIdPermutations(knownComponentIds);

			foreach(var knownCompIdsPermutation in knownComponentIdsPermutations) {
				var compIdsPermutation = new ComponentIdsList();
				compIdsPermutation.UnionWith(knownCompIdsPermutation);
				compIdsPermutation.UnionWith(reqCompIds);

				if(!monoBehaviourTypes.ContainsKey(compIdsPermutation)) {
					monoBehaviourTypes[compIdsPermutation] = new HashSet<Type>();
				}
				monoBehaviourTypes[compIdsPermutation].Add(monoBehaviourType);
			}

			if(!reqCompIds.Any()) {
				monoBehaviourTypes[new ComponentIdsList()].Add(monoBehaviourType);
			}
		}

		private static void RegisterRequiredInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			requiredComponentsMap[monoBehaviourType].Add(componentId);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnInitComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onInitComponentsMap[monoBehaviourType].Add(componentId);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnUpdateComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onUpdateComponentsMap[monoBehaviourType].Add(componentId);
			AddKnownComponentId(componentId);
		}

		private static void RegisterOnRemoveComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onRemoveComponentsMap[monoBehaviourType].Add(componentId);
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
					EcsIdl.Util.GetComponentType(componentId)!
				);

				var newMonoBehaviourTypes = new ComponentIdsTypesMap();

				foreach(var (key, value) in monoBehaviourTypes) {
					if(!key.Any()) continue;
					var newComponentIds = new ComponentIdsList(key);
					newComponentIds.Add(componentId);

					newMonoBehaviourTypes.Add(
						newComponentIds,
						new HashSet<System.Type>(value)
					);
				}

				newMonoBehaviourTypes.ToList().ForEach(
					item => monoBehaviourTypes.Add(item.Key, item.Value)
				);
			}
		}
	}
}
