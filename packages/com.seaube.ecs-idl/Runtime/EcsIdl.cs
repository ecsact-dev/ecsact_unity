using System;
using System.Reflection;

namespace EcsIdl {
	/// <summary>ECS IDL Component Marker Interface</summary>
	public interface Component {}

	/// <summary>ECS IDL Action Marker Interface</summary>
	public interface Action {}

	public static class Util {

		public static System.Int32 GetComponentID<T>() where T : EcsIdl.Component {
			return GetComponentID(typeof(T));
		}

		public static System.Int32 GetComponentID
			( System.Type componentType
			)
		{
			return (System.Int32)componentType
				.GetProperty("id", BindingFlags.Static | BindingFlags.Public)
				.GetValue(null, null);
		}

	}
}
