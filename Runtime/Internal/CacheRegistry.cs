

// NOTE: Do fancy assembly stuff
namespace Ecsact.Internal {
public class CacheRegistry {
	private Ecsact.Registry reg;

	public CacheRegistry(EcsactRuntime runtime, Ecsact.Registry registry) {
		reg = registry;

		runtime.OnEntityCreated((entityId, placeholderId) => {
			UnityEngine.Debug.Log("Entity created");
			reg.EnsureEntity(entityId);
		});

		runtime.OnInitComponent((entityId, componentId, component) => {
			reg.EnsureEntity(entityId);
			reg.AddComponent(entityId, componentId, component);
		});

		runtime.OnUpdateComponent((entityId, componentId, component) => {
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
