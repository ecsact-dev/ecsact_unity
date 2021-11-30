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
		private static ComponentIdsList knownRequiredComponentIds;
		private static TypeComponentIdsMap requiredComponentsMap;
		private static TypeComponentIdsMap onInitComponentsMap;
		private static TypeComponentIdsMap onUpdateComponentsMap;
		private static TypeComponentIdsMap onRemoveComponentsMap;

		private static ComponentIdsTypesMap monoBehaviourTypes;
		private static ComponentIdsTypesMap requiredMonoBehaviourTypes;

		static UnitySyncMonoBehaviours() {
			knownComponentIds = new ComponentIdsList();
			knownRequiredComponentIds = new ComponentIdsList();
			requiredComponentsMap = new TypeComponentIdsMap();
			onInitComponentsMap = new TypeComponentIdsMap();
			onUpdateComponentsMap = new TypeComponentIdsMap();
			onRemoveComponentsMap = new TypeComponentIdsMap();
			monoBehaviourTypes = new ComponentIdsTypesMap(
				ComponentIdsList.CreateSetComparer()
			);
			requiredMonoBehaviourTypes = new ComponentIdsTypesMap(
				ComponentIdsList.CreateSetComparer()
			);
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

			return previousTypes.Except(currentTypes);
		}

		public static IEnumerable<Type> GetRemovedTypes
			( ComponentIdsList previousComponentIds
			, ComponentIdsList currentComponentIds
			)
		{
			var previousTypes = GetTypes(previousComponentIds);
			var currentTypes = GetTypes(currentComponentIds);

			return currentTypes.Except(previousTypes);
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
			knownRequiredComponentIds.Clear();
			requiredComponentsMap.Clear();
			onInitComponentsMap.Clear();
			onUpdateComponentsMap.Clear();
			onRemoveComponentsMap.Clear();
			monoBehaviourTypes.Clear();
			requiredMonoBehaviourTypes.Clear();

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

			if(!requiredMonoBehaviourTypes.ContainsKey(reqCompIds)) {
				requiredMonoBehaviourTypes[reqCompIds] = new HashSet<Type>();
			}
			requiredMonoBehaviourTypes[reqCompIds].Add(monoBehaviourType);

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
			AddKnownRequiredComponentId(componentId);
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

		private static void AddKnownRequiredComponentId
			( System.Int32 componentId
			)
		{
			if(knownRequiredComponentIds.Add(componentId)) {
				// New known required component
				// TODO: Add new component id key permutations
			}
		}

		private static void AddKnownComponentId
			( System.Int32 componentId
			)
		{
			if(knownComponentIds.Add(componentId)) {
				// New known component
				// TODO: Add new component id key permutations
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
