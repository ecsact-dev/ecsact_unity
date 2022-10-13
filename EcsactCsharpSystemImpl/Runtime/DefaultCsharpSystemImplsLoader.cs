using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

#nullable enable

namespace EcsactInternal {
	public class DefaultCsharpSystemImplsLoader {
#if UNITY_EDITOR
		public static List<string> ValidateImplMethodInfo
			( global::System.Reflection.MethodInfo methodInfo
			)
		{
			var errors = new List<string>();
			if(!methodInfo.IsStatic) {
				errors.Add("Method is non-static");
			}

			var parameters = methodInfo.GetParameters();
			if(parameters.Length != 1) {
				errors.Add($"Method has {parameters.Length} parameter(s). Expected 1.");
			} else {
				var paramType = parameters[0].ParameterType;
				if(paramType != typeof(EcsactRuntime.SystemExecutionContext)) {
					errors.Add(
						$"Invalid method parameter type {paramType.FullName}. Expected " +
						"EcsactRuntime.SystemExecutionContext."
					);
				}
			}

			var returnType = methodInfo.ReturnType;
			if(returnType != typeof(void)) {
				errors.Add(
					"System implementation may only return void. Instead got " +
					$"{returnType.FullName}"
				);
			}

			return errors;
		}
#endif // UNITY_EDITOR

		public static bool Enabled() {
			var systemImplSource = EcsactRuntimeSettings.Get().systemImplSource;
			return systemImplSource == Ecsact.SystemImplSource.Csharp;
		}

		[RuntimeInitializeOnLoadMethod]
		internal static void Load() {

			if(!Enabled()) {
				return;
			}

			Ecsact.Defaults.WhenReady(() => {
				var runtimeSettings = EcsactRuntimeSettings.Get();
				var runtime = Ecsact.Defaults.Runtime;
				var implsAssembly = Assembly.Load(
					runtimeSettings.defaultCsharpSystemImplsAssemblyName
				);

				foreach(var type in implsAssembly.GetTypes()) {
					foreach(var method in type.GetMethods()) {
						var defaultSystemImplAttr =
							method.GetCustomAttribute<Ecsact.DefaultSystemImplAttribute>();
						if(defaultSystemImplAttr == null) continue;

						var systemLikeId = defaultSystemImplAttr.systemLikeId;

#if UNITY_EDITOR
					var errors = ValidateImplMethodInfo(method);
					if(errors.Count > 0) {
						var methodName = method.DeclaringType.FullName + "." + method.Name;
						var errorMessage =
							$"Invalid Ecsact system impl method <b>{methodName}</b>:\n";
						foreach(var error in errors) {
							errorMessage += $"\n - <color=red>{error}</color>";
						}

						Debug.LogError(
							message: errorMessage,
							context: runtimeSettings
						);
						continue;
					}
#endif // UNITY_EDITOR

						var implDelegate = Delegate.CreateDelegate(
							type: typeof(EcsactRuntime.SystemExecutionImpl),
							method: method
						) as EcsactRuntime.SystemExecutionImpl;
						Debug.Assert(implDelegate != null);
						runtime.dynamic.SetSystemExecutionImpl(systemLikeId, implDelegate!);
					}
				}		
			});
		}
	}
}
