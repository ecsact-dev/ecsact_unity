using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

#nullable enable

namespace Ecsact {
	/// <summary>ECSACT Component Marker Interface</summary>
	public interface Component {}

	/// <summary>ECSACT Action Marker Interface</summary>
	public interface Action {}

	/// <summary>ECSACT System Marker Interface</summary>
	public interface System {}

	public static class Util {
		private static Dictionary<Int32, Type> cachedComponentTypes;

		static Util() {
			cachedComponentTypes = new Dictionary<Int32, Type>();
		}

		public static bool IsComponent
			( Type componentType
			)
		{
			foreach(var i in componentType.GetInterfaces()) {
				if(i == typeof(Ecsact.Component)) {
					return true;
				}
			}

			return false;
		}

		public static bool IsSystem
			( Type systemType
			)
		{
			foreach(var i in systemType.GetInterfaces()) {
				if(i == typeof(Ecsact.System)) {
					return true;
				}
			}

			return false;
		}

		public static bool IsAction
			( Type actionType
			)
		{
			foreach(var i in actionType.GetInterfaces()) {
				if(i == typeof(Ecsact.Action)) {
					return true;
				}
			}

			return false;
		}

		public static object? PtrToComponent
			( IntPtr  componentIntPtr
			, Int32   componentId
			)
		{
			var componentType = GetComponentType(componentId);

			if(componentType != null) {
				if(componentIntPtr == IntPtr.Zero) {
					return Activator.CreateInstance(componentType);
				}
				return Marshal.PtrToStructure(componentIntPtr, componentType);
			}

			return null;
		}

		public static object PtrToComponent<C>
			( IntPtr componentPtr
			) where C : Ecsact.Component
		{
			if(componentPtr == IntPtr.Zero) {
				return Activator.CreateInstance(typeof(C));
			}

			return Marshal.PtrToStructure(componentPtr, typeof(C));
		}

		public static object HandlePtrToComponent
			(ref object? componentObject
			)
		{
			if(componentObject is null) {
				throw new Exception("Unknown component ID used");
			} else {
				return componentObject;
			}
		}

		public static void ComponentToPtr
			( object  component
			, Int32   componentId
			, IntPtr  componentIntPtr
			)
		{
			Marshal.StructureToPtr(component, componentIntPtr, false);
		}

		public static Type? GetComponentType
			( Int32 componentId
			)
		{
			if(cachedComponentTypes.TryGetValue(componentId, out var t)) {
				return t;
			}

			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach(var type in assembly.GetTypes()) {
					if(IsComponent(type)) {
						var typeComponentId = GetComponentID(type);
						if(typeComponentId == componentId) {
							return type;
						}
					}
				}
			}

			return null;
		}

		public static void ClearComponentTypeCache() {
			cachedComponentTypes.Clear();
		}

		public static Int32 GetComponentID<T>() where T : Ecsact.Component {
			return GetComponentID(typeof(T));
		}

		public static Int32 GetComponentID
			( Type componentType
			)
		{
			if(!IsComponent(componentType)) {
				throw new ArgumentException("Invalid component type");
			}

			var idField = componentType.GetField(
				"id",
				BindingFlags.Static | BindingFlags.Public
			);

			var componentId = (Int32)idField.GetValue(null);
			cachedComponentTypes[componentId] = componentType;
			return componentId;
		}

		public static Int32 GetActionID<T>() where T : Ecsact.Action {
			return GetActionID(typeof(T));
		}

		public static Int32 GetActionID
			( Type actionType
			)
		{
			return GetSystemID(actionType);
		}

		public static Int32 GetSystemID<T>() where T : Ecsact.System {
			return GetSystemID(typeof(T));
		}

		public static Int32 GetSystemID
			( Type actionType
			)
		{
			var idField = actionType.GetField(
				"id",
				BindingFlags.Static | BindingFlags.Public
			);

			return (Int32)idField.GetValue(null);
		}

		public static IEnumerable<ComponentIdsList> GetComponentIdPermutations
			( ComponentIdsList componentIds
			)
		{
			// Adapted originally from https://stackoverflow.com/a/42842770
			var componentIdsList = new List<Int32>(componentIds);
			var count = componentIds.Count;
			if(count == 0) yield return new ComponentIdsList();

			UnityEngine.Debug.Log("PERMUTATION START");
			for(;count > 0; --count) {
				if( count == componentIds.Count ) yield return componentIds;
				if( count > componentIds.Count ) yield break;
				var ptrs = Enumerable.Range(0, count).ToArray();

				while(ptrs[0] <= componentIdsList.Count - count) {
					var permutationList = new ComponentIdsList(
						ptrs.Select(p => componentIdsList[p])
					);
					yield return permutationList;
					string msg = "\t{";
					foreach(var id in permutationList) {
						msg += id + ", ";
					}
					msg = msg.TrimEnd(new char[]{',', ' '});
					msg += "}";
					UnityEngine.Debug.Log(msg);

					++ptrs[count - 1];

					int i = count - 2;
					while(ptrs[count - 1] >= componentIdsList.Count && i >= 0) {
						++ptrs[i];

						for(int j = i + 1; j < count; ++j) {
							ptrs[j] = ptrs[j - 1] + 1;
						}

						--i;
					}
				}
			}
			UnityEngine.Debug.Log("PERMUTATION END");
		}

		public static IEnumerable<Type> GetAllSystemLikeTypes() {
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach(var type in assembly.GetTypes()) {
					if(Util.IsSystem(type) || Util.IsAction(type)) {
						yield return type;
					}
				}
			}
		}
	}
}
