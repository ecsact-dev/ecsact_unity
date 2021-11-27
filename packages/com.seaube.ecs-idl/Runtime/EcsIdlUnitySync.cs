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
		private static TypeComponentIdsMap requiredComponentsMap;
		private static TypeComponentIdsMap onInitComponentsMap;
		private static TypeComponentIdsMap onUpdateComponentsMap;
		private static TypeComponentIdsMap onRemoveComponentsMap;

		static UnitySyncMonoBehaviours() {
			requiredComponentsMap = new TypeComponentIdsMap();
			onInitComponentsMap = new TypeComponentIdsMap();
			onUpdateComponentsMap = new TypeComponentIdsMap();
			onRemoveComponentsMap = new TypeComponentIdsMap();
		}

		public static void TriggerOnInit
			( GameObject  gameObject
			, SortedSet<System.Int32> prevComponentIds
			, SortedSet<System.Int32> currentComponentIds
			)
		{

		}

		// public static IEnumerator<Type> GetMonoBehaviourTypes
		// 	( SortedSet<System.Int32> components
		// 	)
		// {

		// }

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
			
		}

		private static void RegisterRequiredInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			requiredComponentsMap[monoBehaviourType].Add(componentId);
		}

		private static void RegisterOnInitComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onInitComponentsMap[monoBehaviourType].Add(componentId);
		}

		private static void RegisterOnUpdateComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onUpdateComponentsMap[monoBehaviourType].Add(componentId);
		}

		private static void RegisterOnRemoveComponentInterface
			( Type monoBehaviourType
			, Type componentType
			)
		{
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			onRemoveComponentsMap[monoBehaviourType].Add(componentId);
		}
	}
}
