using UnityEngine;
using System.Collections.Generic;

#nullable enable

namespace Ecsact {
	[global::System.Serializable]
	public class SerializableEcsactComponent : ISerializationCallbackReceiver {
#if UNITY_EDITOR
		[SerializeField, HideInInspector]
		internal string? _ecsactComponentNameEditorOnly;
#endif

		[SerializeField, HideInInspector]
		public global::System.Int32 id;
		public object? data;
		public string? _dataJson;

		public void OnAfterDeserialize() {
			if(_dataJson == null) return;

			var componentType = Util.GetComponentType(id);

#if UNITY_EDITOR
			// If the name of the component type does not match the stored one we may
			// have had our component ID change on us.
			var componentNameDoesNotMatch =
				componentType == null ||
				componentType.FullName != _ecsactComponentNameEditorOnly;
			if(componentNameDoesNotMatch) {
				foreach(var otherComponentType in Util.GetAllComponentTypes()) {
					if(otherComponentType.FullName == _ecsactComponentNameEditorOnly) {
						componentType = otherComponentType;
						id = Util.GetComponentID(otherComponentType);
					}
				}
			}
#endif

			data = JsonUtility.FromJson(_dataJson, componentType);

			_dataJson = null;
		}

		public void OnBeforeSerialize() {
			_dataJson = JsonUtility.ToJson(data);
		}
	}

	public class DynamicEntity : MonoBehaviour {
		public global::System.Int32 entityId { get; private set; } = -1;
		public List<SerializableEcsactComponent> ecsactComponents = new();

		private EcsactRuntime? runtime;
		private EcsactRuntimeSettings? settings;
		private EcsactRuntimeDefaultRegistry? defReg;

		public void AddEcsactCompnent<C>
			( C component
			) where C : Ecsact.Component
		{
			if(Application.isPlaying) {
				runtime!.core.AddComponent(defReg!.registryId, entityId, component);
			}

			ecsactComponents.Add(new SerializableEcsactComponent{
				id = Util.GetComponentID<C>(),
				data = component,
			});
		}

		public void AddEcsactComponent
			( global::System.Int32  componentId
			, object                componentData
			)
		{
			if(Application.isPlaying) {
				runtime!.core.AddComponent(
					defReg!.registryId,
					entityId,
					componentId,
					componentData
				);
			}

			ecsactComponents.Add(new SerializableEcsactComponent{
				id = componentId,
				data = componentData,
			});
		}

		void OnEnable() {
			runtime = EcsactRuntime.GetOrLoadDefault();
			settings = EcsactRuntimeSettings.Get();
			defReg = settings.defaultRegistries[0];
			
			entityId = runtime.core.CreateEntity(defReg.registryId);

			foreach(var ecsactComponent in ecsactComponents) {
				runtime.core.AddComponent(
					defReg.registryId,
					entityId,
					ecsactComponent.id,
					ecsactComponent.data!
				);
			}
		}

		void OnDisable() {
			foreach(var ecsactComponent in ecsactComponents) {
				var hasComponent = runtime!.core.HasComponent(
					defReg!.registryId,
					entityId,
					ecsactComponent.id
				);
				if(hasComponent) {
					runtime!.core.RemoveComponent(
						defReg!.registryId,
						entityId,
						ecsactComponent.id
					);
				}
			}
		}
	};
}
