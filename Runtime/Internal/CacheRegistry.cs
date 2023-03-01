

// NOTE: Do fancy assembly stuff
namespace Ecsact.Internal {
public class CacheRegistry {
	private Ecsact.Registry reg;

	// NOTE: Make add, update and remove internal
	// Remove self invoked callbacks for init, update and remove
	// Issue for a unified read-only registry that can be used in async

	public CacheRegistry(EcsactRuntime runtime, Ecsact.Registry registry) {
		reg = registry;

		runtime.OnEntityCreated((entityId, placeholderId) => {
			UnityEngine.Debug.Log("Entity created");
			reg.EnsureEntity(entityId);
		});

		runtime.OnInitComponent((entityId, componentId, component) => {
			UnityEngine.Debug.Log("Init Component");
			reg.AddComponent(entityId, componentId, component);
		});

		runtime.OnUpdateComponent((entityId, componentId, component) => {
			UnityEngine.Debug.Log("Update Component");
			reg.UpdateComponent(entityId, componentId, component);
		});

		runtime.OnRemoveComponent((entityId, componentId, component) => {
			reg.RemoveComponent(entityId, componentId);
		});

		runtime.OnEntityDestroyed((entityId, placeholderId) => {
			reg.DestroyEntity(entityId);
		});
	}
};
}
