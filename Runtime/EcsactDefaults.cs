using System.Runtime.CompilerServices;
using System;

#nullable enable

[assembly:InternalsVisibleTo("EcsactRuntimeDefaults")]

namespace Ecsact {

static public class Defaults {
	internal static EcsactRuntime? _Runtime;
	public static EcsactRuntime Runtime =>
		_Runtime ??
		throw new Exception(
			"Runtime is null, if you want to access it as early as possible " +
			"use Ecsact.Defaults.WhenReady"
		);
	internal static Ecsact.Registry? _Registry;
	public static Ecsact.Registry Registry =>
		_Registry ??
		throw new Exception(
			"Registry is null, if you want to access it as early as possible " +
			"use Ecsact.Defaults.WhenReady"
		);
	public static Ecsact.UnitySync.EntityGameObjectPool? Pool;
	public static EcsactRunner? Runner;

	private static event global::System.Action? onReady;

	public static void WhenReady(global::System.Action callback) {
		if(!UnityEngine.Application.isPlaying) {
			throw new Exception(
				"Ecsact.Defaults.WhenReady may only be used during play mode"
			);
		}
		if(_Runtime != null) {
			callback();
		} else {
			onReady += callback;
		}
	}

	internal static void NotifyReady() {
		if(_Runtime != null) {
			onReady?.Invoke();
			onReady = null;
		} else {
			throw new Exception(
				"Cannot notify ready until the Ecsact Runtime is ready"
			);
		}
	}

	internal static void ClearDefaults() {
		if(_Runtime != null) {
			EcsactRuntime.Free(_Runtime);
			Runner = null;
			_Runtime = null;
			_Registry = null;
			Pool = null;
		}
	}
}

} // namespace Ecsact
