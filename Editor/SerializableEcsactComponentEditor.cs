using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Ecsact.Editor {
	[CustomPropertyDrawer(typeof(Ecsact.SerializableEcsactComponent))]
	public class SerializableEcsactComponentDrawer : UnityEditor.PropertyDrawer {
		private static List<Type>? componentTypes;
		private static List<EcsactPackage>? ecsactPackages;

		static IEnumerable<EcsactPackage> FindEcsactPackages() {
			var guids = AssetDatabase.FindAssets($"t:{typeof(EcsactPackage)}");
			foreach (var t in guids) {
				var assetPath = AssetDatabase.GUIDToAssetPath(t);
				var asset = AssetDatabase.LoadAssetAtPath<EcsactPackage>(assetPath);
				if (asset != null) {
					yield return asset;
				}
			}
		}

		static EcsactPackage.Component? FindEcsactComponent
			( string componentName
			)
		{
			Debug.Assert(ecsactPackages != null);
			foreach(var pkg in ecsactPackages!) {
				foreach(var compInfo in pkg.components) {
					if(compInfo.full_name == componentName) {
						return compInfo;
					}
				}
			}

			return null;
		}

		public override float GetPropertyHeight
			( SerializedProperty  property
			, GUIContent          label
			)
		{
			var fieldHeight = 24;

			if(ecsactPackages == null) {
				ecsactPackages = FindEcsactPackages().ToList();
			}

			var componentId = property.FindPropertyRelative("id").intValue;
			var componentName = "";
			global::System.Type? componentType = null;
			if(componentId != -1) {
				componentType = Util.GetComponentType(componentId);
				if(componentType != null) {
					componentName = componentType.FullName;

					var esactComponent = FindEcsactComponent(componentName);
					if(esactComponent != null) {
						var fieldsHeight = fieldHeight;
						foreach(var field in esactComponent.fields) {
							fieldsHeight += fieldHeight * field.field_type.length;
						}
						return fieldsHeight;
					}
				}
			}

			return fieldHeight;
		}

		void DrawEcsactFieldInput
			( Rect                     position
			, ref object               compositeData
			, EcsactPackage.FieldInfo  fieldInfo
			)
		{
			var type = compositeData.GetType();
			var field = type.GetField(fieldInfo.field_name);
			if(fieldInfo.field_type.type == "i32") {
				EditorGUI.IntField(position, fieldInfo.field_name, 0);
			} else if(fieldInfo.field_type.type == "f32") {
				var value = (float)field.GetValue(compositeData);
				value = EditorGUI.FloatField(position, fieldInfo.field_name, value);
				field.SetValue(compositeData, value);
			} else {
				EditorGUI.PrefixLabel(position, new GUIContent(fieldInfo.field_name));
			}
		}

		void DrawEcsactComponentFieldInputs
			( Rect                     position
			, ref object               componentData
			, EcsactPackage.Component  componentInfo
			)
		{
			foreach(var fieldInfo in componentInfo.fields) {
				DrawEcsactFieldInput(position, ref componentData, fieldInfo);
				position.position += new Vector2{y=24};
			}
		}

		public override void OnGUI
			( Rect                position
			, SerializedProperty  property
			, GUIContent          label
			)
		{
			if(ecsactPackages == null) {
				ecsactPackages = FindEcsactPackages().ToList();
			}

			var componentId = property.FindPropertyRelative("id").intValue;
			var componentName = "(none)";
			global::System.Type? componentType = null;
			if(componentId != -1) {
				componentType = Util.GetComponentType(componentId);
				if(componentType != null) {
					componentName = componentType.FullName;
				}
			}

			EditorGUI.BeginProperty(position, label, property);
			var prefixLabelRect = new Rect(position.x, position.y, 120, 20);
			EditorGUI.PrefixLabel(
				prefixLabelRect,
				new GUIContent("Component Type: ")
			);
			var dropdownBtnRect = new Rect(prefixLabelRect);
			dropdownBtnRect.position =
				prefixLabelRect.position + new Vector2{x=prefixLabelRect.width, y=1};
			var dropdownPressed = EditorGUI.DropdownButton(
				dropdownBtnRect,
				new GUIContent(componentName),
				focusType: FocusType.Keyboard
			);

			if(componentType != null) {
				var fieldRect = new Rect(position);
				fieldRect.position += new Vector2{y=prefixLabelRect.height};
				var dataJsonProp = property.FindPropertyRelative("_dataJson");
				var componentData = JsonUtility.FromJson(
					dataJsonProp.stringValue,
					componentType
				);

				var ecsactCompnent = FindEcsactComponent(componentName);
				if(ecsactCompnent != null) {
					var componentFieldInputsRect = new Rect(prefixLabelRect);
					componentFieldInputsRect.position += new Vector2{y=24};
					componentFieldInputsRect.width = position.width;
					DrawEcsactComponentFieldInputs(
						componentFieldInputsRect,
						ref componentData,
						ecsactCompnent
					);

					dataJsonProp.stringValue = JsonUtility.ToJson(componentData);
				}
			}

			if(dropdownPressed) {
				var dropdownMenu = new GenericMenu();
				if(componentTypes == null) {
					componentTypes = Util.GetAllComponentTypes().ToList();
				}

				foreach(var componentTypeOption in componentTypes) {
					var componentIdOption = Util.GetComponentID(componentTypeOption);
					dropdownMenu.AddItem(
						new GUIContent(componentTypeOption.FullName),
						false,
						() => {
							var compIdProp = property.FindPropertyRelative("id");
							var compNameProp = property.FindPropertyRelative(
								"_ecsactComponentNameEditorOnly"
							);
							var dataJsonProp = property.FindPropertyRelative("_dataJson");
							compIdProp.intValue = componentIdOption;
							compNameProp.stringValue = componentTypeOption.FullName;
							dataJsonProp.stringValue = JsonUtility.ToJson(
								Activator.CreateInstance(componentTypeOption)
							);
							
							property.serializedObject.ApplyModifiedProperties();
						}
					);
				}

				dropdownMenu.ShowAsContext();
			}

			EditorGUI.EndProperty();
		}
	}
}
