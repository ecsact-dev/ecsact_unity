using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

namespace EcsIdl {
	/// <summary>ECS IDL Component Marker Interface</summary>
	public interface Component {}

	/// <summary>ECS IDL Action Marker Interface</summary>
	public interface Action {}

	public static class Util {

		public static System.Int32 GetComponentID<T>() where T : EcsIdl.Component {
			return GetComponentID(typeof(T));
		}

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

		public static IEnumerable<ComponentIdsList> GetComponentIdPermutations
			( ComponentIdsList componentIds
			)
		{
			// Adapted originally from https://stackoverflow.com/a/42842770
			var componentIdsList = new List<Int32>(componentIds);
			var count = componentIds.Count;
			if(count == 0) yield return new ComponentIdsList();

			for(;count > 0; --count) {
				if( count == componentIds.Count ) yield return componentIds;
				if( count > componentIds.Count ) yield break;
				var ptrs = Enumerable.Range(0, count).ToArray();

				while(ptrs[0] <= componentIdsList.Count - count) {
					yield return new ComponentIdsList(
						ptrs.Select(p => componentIdsList[p])
					);

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
		}

	}
}
