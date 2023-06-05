using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("EcsactRuntimeDefaults")]

namespace Ecsact.Internal {
internal class CacheRegistry {
	private Ecsact.Registry reg;

	public CacheRegistry(EcsactRuntime runtime, Ecsact.Registry registry) {
		reg = registry;

		runtime.OnEntityCreated((entityId, placeholderId) => {
			reg.EnsureEntity(entityId);
		});

		runtime.OnInitComponent((entityId, componentId, component) => {
			try {
				if(reg.HasComponent(entityId, componentId)) {
					reg.UpdateComponent(entityId, componentId, component);
				} else {
					reg.AddComponent(entityId, componentId, component);
				}
			} catch(global::System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		});

		runtime.OnUpdateComponent((entityId, componentId, component) => {
			try {
				if(reg.HasComponent(entityId, componentId)) {
					reg.UpdateComponent(entityId, componentId, component);
				} else {
					reg.AddComponent(entityId, componentId, component);
				}
			} catch(global::System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		});

		runtime.OnRemoveComponent((entityId, componentId, component) => {
			try {
				if(reg.HasComponent(entityId, componentId)) {
					reg.RemoveComponent(entityId, componentId);
				}
			} catch(global::System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		});

		runtime.OnEntityDestroyed((entityId, placeholderId) => {
			try {
				if(reg.EntityExists(entityId)) {
					reg.DestroyEntity(entityId);
				}
			} catch(global::System.Exception err) {
				UnityEngine.Debug.LogException(err);
			}
		});
	}
};
}
