using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

#nullable enable

namespace EcsIdl {
	/// <summary>ECS IDL Component Marker Interface</summary>
	public interface Component {}

	/// <summary>ECS IDL Action Marker Interface</summary>
	public interface Action {}

	public static class Util {

		public static bool IsComponent
			( System.Type componentType
			)
		{
			foreach(var i in componentType.GetInterfaces()) {
				if(i == typeof(EcsIdl.Component)) {
					return true;
				}
			}

			return false;
		}

		public static System.Type? GetComponentType
			( System.Int32 componentId
			)
		{
			foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
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

		public static System.Int32 GetComponentID<T>() where T : EcsIdl.Component {
			return GetComponentID(typeof(T));
		}

		public static System.Int32 GetComponentID
			( System.Type componentType
			)
		{
			var idField = componentType.GetField(
				"id",
				BindingFlags.Static | BindingFlags.Public
			);

			return (System.Int32)idField.GetValue(null);
		}

		public static System.Int32 GetActionID<T>() where T : EcsIdl.Action {
			return GetActionID(typeof(T));
		}

		public static System.Int32 GetActionID
			( System.Type actionType
			)
		{
			var idField = actionType.GetField(
				"id",
				BindingFlags.Static | BindingFlags.Public
			);

			return (System.Int32)idField.GetValue(null);
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

	}
}
