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
		[SerializeField, HideInInspector]
		public List<string> entityFieldNames = new();
		[SerializeField, HideInInspector]
		public List<DynamicEntity?> otherEntities = new();
		[SerializeField, HideInInspector]
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
	
		private EcsactRuntimeDefaultRegistry? defReg;

		public void AddEcsactCompnent<C>
			( C component
			) where C : Ecsact.Component
		{
			if(Application.isPlaying) {
				Ecsact.Defaults.Registry!.AddComponent(entityId, component);
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
				Ecsact.Defaults.Registry.AddComponent(
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

		private void CreateEntityIfNeeded() {
			if(entityId == -1) {
				entityId = Ecsact.Defaults.Registry.CreateEntity();
				if(defReg.pool != null) {
					defReg.pool.SetPreferredEntityGameObject(entityId, gameObject);
				}
			}
		}

		private void AddInitialEcsactComponents() {
			foreach(var ecsactComp in ecsactComponents) {
				for(int i=0; ecsactComp.otherEntities.Count > i; ++i) {
					var otherEntity = ecsactComp.otherEntities[i];
					if(otherEntity != null && otherEntity.entityId == -1) {
						otherEntity.CreateEntityIfNeeded();
						var fieldName = ecsactComp.entityFieldNames[i];
						var compType = ecsactComp.data!.GetType();
						var field = compType.GetField(fieldName);
						Debug.Assert(field != null, this);
						field!.SetValue(ecsactComp.data, otherEntity.entityId);
					}
				}
			}

			foreach(var ecsactComponent in ecsactComponents) {
				Ecsact.Defaults.Registry.AddComponent(
					entityId,
					ecsactComponent.id,
					ecsactComponent.data!
				);
			}
		}

		void OnEnable() {			
			CreateEntityIfNeeded();
			AddInitialEcsactComponents();
		}

		void OnDisable() {
			foreach(var ecsactComponent in ecsactComponents) {
				var hasComponent = Ecsact.Defaults.Registry.HasComponent(
					entityId,
					ecsactComponent.id
				);
				if(hasComponent) {
					Ecsact.Defaults.Registry.RemoveComponent(
						entityId,
						ecsactComponent.id
					);
				}
			}
		}
	};
}
