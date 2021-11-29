using System;
using UnityEngine;
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
	, System.Collections.Generic.List<System.Type>
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

	[System.AttributeUsage(System.AttributeTargets.Class)]
	public class SyncedMonoBehaviourAttribute : System.Attribute {
		public SyncedMonoBehaviourAttribute() {}
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
		/// Entity <c>GameObject</c> when it has <c>componentIds</c>.
		public static IEnumerable<Type> GetTypes
			( ComponentIdsList componentIds
			)
		{
			var compIds = new ComponentIdsList(componentIds);

			if(monoBehaviourTypes.TryGetValue(componentIds, out var types)) {
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
			
		}

		public static IEnumerable<Type> GetRemovedTypes
			( ComponentIdsList previousComponentIds
			, ComponentIdsList currentComponentIds
			)
		{
			
		}

		public static void RegisterMonoBehaviourType<T>() where T : MonoBehaviour {
			RegisterMonoBehaviourType(typeof(T));
		}

		public static void RegisterMonoBehaviourType
			( Type type
			)
		{
			// Monobehaviours only
			if(!type.IsAssignableFrom(typeof(MonoBehaviour))) return;

			var registrationBegan = false;

			void beginRegistrationIfNeeded() {
				if(registrationBegan) return;

				registrationBegan = true;
				BeginMonoBehaviourRegistration(type);
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
				requiredMonoBehaviourTypes[reqCompIds] = new List<Type>();
			}
			requiredMonoBehaviourTypes[reqCompIds].Add(monoBehaviourType);

			// TODO: Add only required types + all permutations of non-required types
			if(!monoBehaviourTypes.ContainsKey(compIds)) {
				monoBehaviourTypes[compIds] = new List<Type>();
			}
			monoBehaviourTypes[compIds].Add(monoBehaviourType);
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
			}
		}
	}
}
