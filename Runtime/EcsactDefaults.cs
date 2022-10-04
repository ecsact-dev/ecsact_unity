using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcsactRuntimeDefaults")]

namespace Ecsact {
	static public class Defaults {
		public static EcsactRuntime? Runtime;
		// NOTE(KELWAN) Default registry is currently guaranteed
		public static Ecsact.Registry Registry;
		public static Ecsact.UnitySync.EntityGameObjectPool? Pool;
		public static EcsactRunner Runner;

		private static global::System.Action onReady;

		public static void WhenReady
			( global::System.Action callback
			)
		{
			if(Runtime != null) {
				callback();
			} else {
				onReady += callback;
			}
		}

		internal static void IsReady() {
			if(Runtime != null) {
				onReady.Invoke();
			} else {
				// throw something?
			}
		}
	}

}
