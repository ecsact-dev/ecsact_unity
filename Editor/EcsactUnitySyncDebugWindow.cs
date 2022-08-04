using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ecsact.UnitySync;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using ComponentIdsList = System.Collections.Generic.SortedSet<System.Int32>;

#nullable enable

class EcsactUnitySyncGameObjectPreview : PreviewSceneStage {

	public static EcsactUnitySyncGameObjectPreview CreateInstance() {
		return (EcsactUnitySyncGameObjectPreview)PreviewSceneStage.CreateInstance(
			typeof(EcsactUnitySyncGameObjectPreview)
		);
	}

	public EntityGameObjectPool? pool;
	public event System.Action? onOpenStage;

	protected override void OnEnable() {
		base.OnEnable();
		scene = EditorSceneManager.NewPreviewScene();
		pool = EntityGameObjectPool.CreateInstance(null!);
		pool.targetScene = scene;

		if(Camera.main != null) {
			SceneManager.MoveGameObjectToScene(
				Instantiate(Camera.main.gameObject),
				scene
			);
		}
	}

	protected override bool OnOpenStage() {
		onOpenStage?.Invoke();
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

public class EcsactUnitySyncDebugWindow : EditorWindow {
	static bool allMonoBehavioursFoldout = false;
	static bool refreshing = false;
	static Vector2 scrollPosition = new Vector2();
	static ComponentIdsList testComponentIds = new ComponentIdsList();

	[MenuItem("Window/ECSACT/Unity Sync Debug")]
	static void Init() {
		var window = EditorWindow.GetWindow(typeof(EcsactUnitySyncDebugWindow));
		window.Show();
	}

	private List<System.Type> allMonoBehaviourTypes = new List<System.Type>();
	private List<System.Type> monoBehaviourTypes = new List<System.Type>();
	private List<System.Type> componentTypes = new List<System.Type>();
	private EcsactUnitySyncGameObjectPreview? previewSceneStage;

	void OnEnable() {
		titleContent = new GUIContent("Unity Sync Debug");
		StartRefresh();

		CompilationPipeline.compilationFinished += OnCompilationFinished;
	}

	void OnDisable() {
		CompilationPipeline.compilationFinished -= OnCompilationFinished;
		if(previewSceneStage != null) {
			previewSceneStage.onOpenStage -= OnOpenPreviewStage;
			previewSceneStage = null;
		}
	}

	void OnCompilationFinished
		( object _
		)
	{
		StartRefresh();
	}

	void StartRefresh() {
		refreshing = true;
		EditorCoroutineUtility.StartCoroutine(Refresh(), this);
	}

	IEnumerator<string> Refresh() {
		int progressId = Progress.Start("Finding ECSACT types");
		bool cancelledRequested = false;
		bool cancelled = false;

		Progress.RegisterCancelCallback(progressId, () => {
			if(cancelled) return true;

			cancelledRequested = true;
			return false;
		});

		try {
			foreach(var (pc, typeName) in RefreshTypes()) {
				if(cancelledRequested) {
					cancelled = true;
					Progress.Cancel(progressId);
					refreshing = false;
					yield break;
				}

				Progress.Report(progressId, pc, typeName);

				yield return "";
			}

			RefreshComponentMonoBehaviourTypes();
		} finally {
			Progress.Remove(progressId);
			refreshing = false;
		}
	}

	IEnumerable<(float, string)> RefreshTypes() {
		if(!EditorApplication.isPlaying) {
			UnitySyncMonoBehaviours.ClearRegisteredMonoBehaviourTypes();
			yield return (0F, "");
		}

		allMonoBehaviourTypes.Clear();
		componentTypes.Clear();

		foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
			foreach(var type in assembly.GetTypes()) {
				if(Ecsact.Util.IsComponent(type)) {
					componentTypes.Add(type);
				} else {
					var interfaces = UnitySyncMonoBehaviours.GetInterfaces(type);
					if(interfaces.Any()) {
						allMonoBehaviourTypes.Add(type);
					}
				}
			}
		}

		componentTypes = componentTypes.OrderBy(t => t.FullName).ToList();
		allMonoBehaviourTypes =
			allMonoBehaviourTypes.OrderBy(t => t.FullName).ToList();

		yield return (0F, "");

		if(!EditorApplication.isPlaying) {
			var registerProgress =
				UnitySyncMonoBehaviours.RegisterMonoBehaviourTypes(
					allMonoBehaviourTypes
				);
			int registeredTypes = 0;
			foreach(var type in registerProgress) {
				yield return (
					((float)registeredTypes / (float)allMonoBehaviourTypes.Count()),
					type.FullName
				);
				registeredTypes += 1;
			}
		} else {
			yield return (1F, "");
		}
	}

	void RefreshComponentMonoBehaviourTypes() {
		monoBehaviourTypes.Clear();
		var types = UnitySyncMonoBehaviours.GetTypes(testComponentIds);

		foreach(var type in types) {
			monoBehaviourTypes.Add(type);
		}
	}

	void RefreshPreviewEntityGameObjectPool() {
		if(previewSceneStage == null || previewSceneStage.pool == null) return;
		var pool = previewSceneStage.pool;

		pool.Clear();
		foreach(var componentId in testComponentIds) {
			var componentType = Ecsact.Util.GetComponentType(componentId);
			if(componentType != null) {
				var component = System.Activator.CreateInstance(componentType);
				pool.InitComponent(0, componentId, component);
			} else {
				Debug.LogWarning($"Cannot find component type from id {componentId}");
			}
		}
	}

	void PreviewEntityGameObjectPoolAdd
		( System.Int32 componentId
		)
	{
		if(previewSceneStage == null || previewSceneStage.pool == null) return;
		var pool = previewSceneStage.pool;

		var componentType = Ecsact.Util.GetComponentType(componentId);
		var component = System.Activator.CreateInstance(componentType);
		pool.InitComponent(0, componentId, component);
	}

	void PreviewEntityGameObjectPoolUpdate
		( System.Int32 componentId
		)
	{
		if(previewSceneStage == null || previewSceneStage.pool == null) return;
		var pool = previewSceneStage.pool;
		var componentType = Ecsact.Util.GetComponentType(componentId);
		// Fake component for the sake of preview purposes only
		var component = System.Activator.CreateInstance(componentType);
		pool.UpdateComponent(0, componentId, component);
	}

	void PreviewEntityGameObjectPoolRemove
		( System.Int32 componentId
		)
	{
		if(previewSceneStage == null || previewSceneStage.pool == null) return;
		var pool = previewSceneStage.pool;
		var componentType = Ecsact.Util.GetComponentType(componentId);
		// Fake component for the sake of preview purposes only
		var component = System.Activator.CreateInstance(componentType);
		pool.RemoveComponent(0, componentId, component);
	}

	void OnGUI() {
		if(refreshing) {
			EditorGUILayout.HelpBox(new GUIContent("Refreshing..."));
			return;
		}

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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
					"Implement one or more Ecsact.UnitySync.IRequired<>, " +
					"Ecsact.UnitySync.IOnInitComponent<>, " +
					"Ecsact.UnitySync.IOnUpdateComponent<>, or " +
					"Ecsact.UnitySync.IOnRemoveComponent<> interfaces for your " +
					"MonoBehaviour scripts to be considered for ECSACT Unity Sync.",
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
			var componentId = Ecsact.Util.GetComponentID(componentType);
			var enabled = testComponentIds.Contains(componentId);
			enabled = EditorGUILayout.ToggleLeft(componentType.FullName, enabled);

			if(enabled && !testComponentIds.Contains(componentId)) {
				testComponentIds.Add(componentId);
				RefreshComponentMonoBehaviourTypes();
				PreviewEntityGameObjectPoolAdd(componentId);
				SelectPreviewEntityGameObjectIfNoneActive();
			} else if(!enabled && testComponentIds.Contains(componentId)) {
				testComponentIds.Remove(componentId);
				RefreshComponentMonoBehaviourTypes();
				PreviewEntityGameObjectPoolRemove(componentId);
				SelectPreviewEntityGameObjectIfNoneActive();
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
			EditorApplication.delayCall += () => OpenPreviewScene();
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

		GUILayout.Space(20);

		EditorGUILayout.EndScrollView();
	}

	private void OpenPreviewScene() {
		previewSceneStage = EcsactUnitySyncGameObjectPreview.CreateInstance();
		previewSceneStage.onOpenStage += OnOpenPreviewStage;
		StageUtility.GoToStage(previewSceneStage, true);
	}

	private void OnOpenPreviewStage() {
		RefreshPreviewEntityGameObjectPool();
		SelectPreviewEntityGameObjectIfNoneActive();
	}

	private void SelectPreviewEntityGameObjectIfNoneActive() {
		if(Selection.activeGameObject == null) {
			if(previewSceneStage != null && previewSceneStage.pool != null) {
				var gameObject = previewSceneStage.pool.GetEntityGameObject(0);
				Selection.activeGameObject = gameObject;
			}
		}
	}
}
