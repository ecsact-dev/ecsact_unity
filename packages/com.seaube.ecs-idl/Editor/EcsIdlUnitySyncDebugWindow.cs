using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EcsIdl.UnitySync;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

class EcsIdlUnitySyncGameObjectPreview : PreviewSceneStage {

	public static EcsIdlUnitySyncGameObjectPreview CreateInstance() {
		return (EcsIdlUnitySyncGameObjectPreview)PreviewSceneStage.CreateInstance(
			typeof(EcsIdlUnitySyncGameObjectPreview)
		);
	}
	
	public GameObject gameObject;

	protected override void OnEnable() {
		base.OnEnable();
		scene = EditorSceneManager.NewPreviewScene();
	}

	protected override bool OnOpenStage() {
		gameObject = new GameObject("Preview Entity Game Object");
		SceneManager.MoveGameObjectToScene(gameObject, scene);
		return true;
	}

	protected override void OnDisable() {
		base.OnDisable();
		EditorSceneManager.ClosePreviewScene(scene);
	}

	protected override GUIContent CreateHeaderContent() {
		return new GUIContent("Unity Sync Debug Preview");
	}
}

public class EcsIdlUnitySyncDebugWindow : EditorWindow {
	static bool allMonoBehavioursFoldout = false;
	static ComponentIdsList testComponentIds = new ComponentIdsList();

	[MenuItem("Window/ECS IDL/Unity Sync Debug")]
	static void Init() {
		var window = EditorWindow.GetWindow(typeof(EcsIdlUnitySyncDebugWindow));
		window.Show();
	}

	private List<System.Type> allMonoBehaviourTypes = new List<System.Type>();
	private List<System.Type> monoBehaviourTypes = new List<System.Type>();
	private List<System.Type> componentTypes = new List<System.Type>();
	private EcsIdlUnitySyncGameObjectPreview previewSceneStage;

	void OnEnable() {
		titleContent = new GUIContent("Unity Sync Debug");
		RefreshTypes();
		RefreshComponentMonoBehaviourTypes();

		CompilationPipeline.compilationFinished += OnCompilationFinished;
	}

	void OnDisable() {
		CompilationPipeline.compilationFinished -= OnCompilationFinished;
		UnityEngine.Object.DestroyImmediate(previewSceneStage);
	}

	void OnCompilationFinished
		( object _
		)
	{
		RefreshTypes();
		RefreshComponentMonoBehaviourTypes();
	}

	void RefreshTypes() {
		if(!EditorApplication.isPlaying) {
			UnitySyncMonoBehaviours.ClearRegisteredMonoBehaviourTypes();
		}
		foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
			foreach(var type in assembly.GetTypes()) {
				if(!EditorApplication.isPlaying) {
					UnitySyncMonoBehaviours.RegisterMonoBehaviourType(type);
				}

				if(EcsIdl.Util.IsComponent(type)) {
					componentTypes.Add(type);
				} else {
					var interfaces = UnitySyncMonoBehaviours.GetInterfaces(type);
					if(interfaces.Any()) {
						allMonoBehaviourTypes.Add(type);
					}
				}
			}
		}
	}

	void RefreshComponentMonoBehaviourTypes() {
		monoBehaviourTypes.Clear();
		var types = UnitySyncMonoBehaviours.GetTypes(testComponentIds);

		foreach(var type in types) {
			monoBehaviourTypes.Add(type);
		}
	}

	void OnGUI() {
		allMonoBehavioursFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
			allMonoBehavioursFoldout,
			"All MonoBehaviour Scripts"
		);

		if(allMonoBehavioursFoldout) {
			++EditorGUI.indentLevel;
			if(allMonoBehaviourTypes.Any()) {
				foreach(var monobehaviourType in allMonoBehaviourTypes) {
					EditorGUILayout.LabelField(monobehaviourType.FullName);
				}
			} else {
				EditorGUILayout.LabelField("(none)");
				EditorGUILayout.HelpBox(
					"Implement one or more EcsIdl.UnitySync.IRequired<>, " +
					"EcsIdl.UnitySync.IOnInitComponent<>, " +
					"EcsIdl.UnitySync.IOnUpdateComponent<>, or " +
					"EcsIdl.UnitySync.IOnRemoveComponent<> interfaces for your " +
					"MonoBehaviour scripts to be considered for ECS IDL Unity Sync.",
					MessageType.Info
				);
			}
			--EditorGUI.indentLevel;
		}

		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space();

		++EditorGUI.indentLevel;
		EditorGUILayout.LabelField("Test Components", EditorStyles.boldLabel);
		foreach(var componentType in componentTypes) {
			var componentId = EcsIdl.Util.GetComponentID(componentType);
			var enabled = testComponentIds.Contains(componentId);
			enabled = EditorGUILayout.ToggleLeft(componentType.FullName, enabled);

			if(enabled && !testComponentIds.Contains(componentId)) {
				testComponentIds.Add(componentId);
				RefreshComponentMonoBehaviourTypes();
			} else if(!enabled && testComponentIds.Contains(componentId)) {
				testComponentIds.Remove(componentId);
				RefreshComponentMonoBehaviourTypes();
			}
		}

		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField("MonoBehaviour Scripts", EditorStyles.boldLabel);

		GUILayout.BeginHorizontal();
		GUILayout.Space(EditorGUI.indentLevel * 20);
		var showPreview = GUILayout.Button("Preview", new GUILayoutOption[]{
			GUILayout.MaxWidth(200)
		});

		if(showPreview) {
			OpenPreviewScene();
		}

		GUILayout.EndHorizontal();

		if(monoBehaviourTypes.Any()) {
			foreach(var monobehaviourType in monoBehaviourTypes) {
				EditorGUILayout.LabelField(monobehaviourType.FullName);
			}
		} else {
			EditorGUILayout.LabelField("(none)");
			EditorGUILayout.HelpBox(
				"No MonoBehaviour scripts will be added to an entity with this " +
				"combination of components. Try adding/removing components in the " +
				"'Test Components' section above.",
				MessageType.Info
			);
		}

		--EditorGUI.indentLevel;
	}

	void OpenPreviewScene() {
		previewSceneStage = EcsIdlUnitySyncGameObjectPreview.CreateInstance();
		StageUtility.GoToStage(previewSceneStage, true);
	}
}
