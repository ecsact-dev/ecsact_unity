using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Ecsact.UnitySync;
using UnityEditor;

#nullable enable

class CurrentSystemExecutionState {
	public static EcsactRuntime? runtime;
}

namespace Ecsact {
	public enum AsyncError : Int32 {
		ConnectionClosed,
		ConnectFail,
		SocketFail,
		StateFail,
		StartFail,
		InvalidConnectionString,
	}
}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
internal static class NativeLibrary {
	private static Dictionary<IntPtr, string> libraryPaths = new();

	[DllImport("Kernel32.dll",
		EntryPoint = "LoadLibrary",
		CharSet = CharSet.Ansi,
		CallingConvention = CallingConvention.Winapi
	)]
	private static extern IntPtr LoadLibrary
		( [MarshalAs(UnmanagedType.LPStr)] string lpFileName
		);

	[DllImport("Kernel32.dll",
		EntryPoint = "FreeLibrary",
		CallingConvention = CallingConvention.Winapi
	)]
	private static extern bool FreeLibrary
		( IntPtr hLibModule
		);

	[DllImport("Kernel32.dll",
		EntryPoint = "GetProcAddress",
		CharSet = CharSet.Ansi,
		CallingConvention = CallingConvention.Winapi
	)]
	private static extern IntPtr GetProcAddress
		( IntPtr                                   hModule
		, [MarshalAs(UnmanagedType.LPStr)] string  procName
		);

	public static IntPtr Load
		( string libraryPath
		)
	{
		UnityEngine.Debug.Log($"Loading Ecsact Runtime: {libraryPath}");
		var libIntPtr = LoadLibrary(libraryPath);
		libraryPaths.Add(libIntPtr, libraryPath);
		return libIntPtr;
	}

	public static void Free
		( IntPtr handle
		)
	{
		var libraryPath = libraryPaths[handle];
		UnityEngine.Debug.Log($"Unloading Ecsact Runtime: {libraryPath}");
		FreeLibrary(handle);
		libraryPaths.Remove(handle);
	}

	public static bool TryGetExport
		( IntPtr      handle
		, string      name
		, out IntPtr  address
		)
	{
		address = GetProcAddress(handle, name);
		return address != IntPtr.Zero;
	}
}
#endif

public class EcsactRuntimeMissingMethod : Exception {
	public EcsactRuntimeMissingMethod
		( string methodName
		)
		: base(methodName)
	{
	}

	public EcsactRuntimeMissingMethod
		( string     methodName
		, Exception  inner
		)
		: base(methodName, inner)
	{
	}
}

public class EcsactRuntime {
	public static class VisualScriptingEventNames {
		public const string AsyncError = "EcsactAsyncErrorEvent";
		public const string AsyncConnectStateChange = "EcsactAsyncConnectStateChange";
	}

	public enum EcsactEvent : Int32 {
		InitComponent = 0,
		UpdateComponent = 1,
		RemoveComponent = 2,
	}

	public delegate void EachComponentCallback
		( Int32   componentId
		, object  componentData
		, IntPtr  callbackUserData
		);

	public delegate void ComponentEventCallback
		( EcsactEvent  ev
		, Int32        entityId
		, Int32        componentId
		, IntPtr       componentData
		, IntPtr       callbackUserData
		);

	public struct EcsactAction {
		public Int32 actionId;
		public IntPtr actionData;
	}

	public struct ExecutionOptions {
		public Int32 addComponentsLength;
		[MarshalAs(UnmanagedType.LPArray)]
		public Int32[] addComponentsEntities;
		[MarshalAs(UnmanagedType.LPArray)]
		public IntPtr[] addComponents;

		public Int32 updateComponentsLength;
		[MarshalAs(UnmanagedType.LPArray)]
		public Int32[] updateComponentsEntities;
		[MarshalAs(UnmanagedType.LPArray)]
		public IntPtr[] updateComponents;

		public Int32 removeComponentsLength;
		[MarshalAs(UnmanagedType.LPArray)]
		public Int32[] removeComponentsEntities;
		[MarshalAs(UnmanagedType.LPArray)]
		public Int32[] removeComponents;

		public Int32 actionsLength;
		[MarshalAs(UnmanagedType.LPArray)]
		public EcsactAction[] actions;
	};

	public struct ExecutionEventsCollector {
		/// <summary>
		/// invoked after system executions are finished for every component that
		/// is new. The component_data is the last value given for the component, 
		/// not the first. Invocation happens in the calling thread. `event` will 
		/// always be <c>EcsactEvent.InitComponent</c>
		/// </summary>
		public ComponentEventCallback initCallback;

		/// <summary>
		/// <c>callbackUserData</c> passed to <c>initCallback</c>
		/// </summary>
		public IntPtr initCallbackUserData;

		/// <summary>
		/// invoked after system executions are finished for every changed 
		/// component. Invocation happens in the calling thread. <c>event</c> will 
		/// always be <c>EcsactEvent.UpdateComponent</c>
		/// </summary>
		public ComponentEventCallback updateCallback;

		/// <summary>
		/// <c>callbackUserData</c> passed to <c>updateCallback</c>
		/// </summary>
		public IntPtr updateCallbackUserData;

		/// <summary>
		/// invoked after system executions are finished for every removed 
		/// component. Invocation happens in the calling thread. <c>event</c> will
		/// always be <c>EcsactEvent.RemoveComponent</c>.
		/// </summary>
		public ComponentEventCallback removeCallback;

		/// <summary>
		/// <c>callbackUserData</c> passed to <c>removeCallback</c>
		/// </summary>
		public IntPtr removeCallbackUserData;
	}

	public struct StaticComponentInfo {
		public Int32 componentId;
		[MarshalAs(UnmanagedType.LPStr)] public string componentName;
		public Int32 componentSize;
		public ComponentCompareFn componentCompareFn;
		[MarshalAs(UnmanagedType.I1)] public bool transient;
	}

	public struct StaticSystemInfo {
		public Int32 systemId;
		public Int32 order;
		[MarshalAs(UnmanagedType.LPStr)] public string systemName;
		public Int32 parentSystemId;
		public Int32 childSystemsCount;
		public Int32[] childSystemIds;
		public Int32 capabilitiesCount;
		public Int32[] capabilityComponents;
		public SystemCapability[] capabilities;
		public CSystemExecutionImpl executionImpl;
	}

	public struct StaticActionInfo {
		public Int32 actionId;
		public Int32 order;
		[MarshalAs(UnmanagedType.LPStr)] public string actionName;
		public Int32 actionSize;
		public ActionCompareFn actionCompareFn;
		public Int32 childSystemsCount;
		public Int32[] childSystemIds;
		public Int32 capabilitiesCount;
		public Int32[] capabilityComponents;
		public SystemCapability[] capabilities;
		public CSystemExecutionImpl executionImpl;
	}

	public delegate void StaticReloadCallback
		( IntPtr userData
		);

	public enum SystemCapability : Int32 {
		Readonly = 1,
		Writeonly = 2,
		ReadWrite = 3,
		OptionalReadonly = 4 | Readonly,
		OptionalWriteonly = 4 | Writeonly,
		OptionalReadWrite = 4 | ReadWrite,
		Include = 8,
		Exclude = 16,
		Adds = 32 | Exclude,
		Removes = 64 | Include,
	}

	public enum SystemGenerate : Int32 {
		Required = 1,
		Optional = 2,
	}

	public struct ContextGenerateBuilder {
		private IntPtr contextPtr;
		private Dictionary<Int32, object> components;

		internal ContextGenerateBuilder
			( IntPtr context
			)
		{
			contextPtr = context;
			components = new();
		}

		public ContextGenerateBuilder AddComponent<C>
			( C component
			) where C : Ecsact.Component
		{
			var componentId = Ecsact.Util.GetComponentID<C>();
			components.Add(componentId, component);
			return this;
		}

		public ContextGenerateBuilder AddManyComponent
			( Dictionary<Int32, object> componentsToAdd
			)
		{
			MergeInPlace(components, componentsToAdd);
			return this;
		}

		public void Finish() {
			var rt = CurrentSystemExecutionState.runtime;

			if(rt is null) {
				throw new Exception("Runtime is invalid");
			}
			if(rt.dynamic.ecsact_system_execution_context_generate == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_generate"
				);
			}

			var componentCount = components.Count;
			if(componentCount == 0) {
				throw new Exception("Can't generate entities with no components (Yet?)");
			}

			if(contextPtr == IntPtr.Zero) {
				UnityEngine.Debug.Log("This is bad!");
			}

			Int32[] componentIds = components.Keys.ToArray();

			List<IntPtr> componentsList = new();

			foreach(var component in components) {
				var componentPtr = 
					Marshal.AllocHGlobal(Marshal.SizeOf(component.Value));

				Ecsact.Util.ComponentToPtr(
					component.Value,
					component.Key,
					componentPtr
				);
				componentsList.Add(componentPtr);
			}

			IntPtr[] componentsData = componentsList.ToArray();

			rt.dynamic.ecsact_system_execution_context_generate(
				contextPtr,
				componentCount,
				componentIds,
				componentsData
			);

			foreach(var componentPtr in componentsData) {
				Marshal.FreeHGlobal(componentPtr);
			}
			components.Clear();
		}

		private Dictionary<Int32, object> MergeInPlace
			( Dictionary<Int32, object> left
			, Dictionary<Int32, object> right
			)
		{
			if (left == null) {
				throw new ArgumentNullException("Can't merge into a null dictionary");
			}
			else if (right == null) {
				return left;
			}

			foreach (var kvp in right) {
				if (!left.ContainsKey(kvp.Key))
				{
					left.Add(kvp.Key, kvp.Value);
				} else {
					throw new Exception(
						"Attempted to duplicate a component in context.Generate"
					);
				}
			}
			return left;
		}
	}

	public struct SystemExecutionContext {
		public SystemExecutionContext
			( IntPtr context
			)
		{
			contextPtr = context;
			rt = CurrentSystemExecutionState.runtime!;
			if(rt is null) {
				throw new Exception(
					"SystemExecution can only be used in a system impl"
				);
			}
		}

		public C Get<C>() where C : Ecsact.Component, new() {
			var rt = CurrentSystemExecutionState.runtime;

			if(rt is null) {
				throw new Exception("Runtime is invalid");
			}
			if(rt.dynamic.ecsact_system_execution_context_get == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_get"
				);
			}

			var componentID = Ecsact.Util.GetComponentID<C>();
			object componentData = new C();

			GCHandle handle = GCHandle.Alloc(componentData, GCHandleType.Pinned);
			IntPtr componentPtr = handle.AddrOfPinnedObject();
			try {
				rt.dynamic.ecsact_system_execution_context_get(
					contextPtr,
					componentID,
					componentPtr
				);
			} finally {
				handle.Free();
			}

			var componentObject =
					Ecsact.Util.PtrToComponent<C>(componentPtr);

			var component = (C)componentObject;

			return component;
		}

		public void Add<C>
			( C component
			) where C : Ecsact.Component
		{
			if(rt.dynamic.ecsact_system_execution_context_add == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_add"
				);
			}

			var componentId = Ecsact.Util.GetComponentID<C>();
			var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

			try {
				Marshal.StructureToPtr(component, componentPtr, false);
				rt.dynamic.ecsact_system_execution_context_add(
					contextPtr,
					componentId,
					componentPtr
				);
			} finally {
				Marshal.FreeHGlobal(componentPtr);
			}
		}

		public void Remove<C>() where C : Ecsact.Component {
			if(rt.dynamic.ecsact_system_execution_context_remove == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_remove"
				);
			}

			var componentId = Ecsact.Util.GetComponentID<C>();

			rt.dynamic.ecsact_system_execution_context_remove(
				contextPtr,
				componentId
			);
		}

		public ContextGenerateBuilder Generate() {
			return new ContextGenerateBuilder(contextPtr);
		}

		public bool Has<C>() where C : Ecsact.Component {
			if(rt.dynamic.ecsact_system_execution_context_has == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_has"
				);
			}

			var componentID = Ecsact.Util.GetComponentID<C>();

			return rt.dynamic.ecsact_system_execution_context_has(
				contextPtr,
				componentID
			);
		}

		public Int32 ID() {
			if(rt.dynamic.ecsact_system_execution_context_id == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_id"
				);
			}

			return rt.dynamic.ecsact_system_execution_context_id(contextPtr);
		}

		public SystemExecutionContext Parent() {
			if(rt.dynamic.ecsact_system_execution_context_parent == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_parent"
				);
			}

			var parentPtr = rt.dynamic.ecsact_system_execution_context_parent(
				contextPtr
			);

			var context = new SystemExecutionContext(parentPtr);

			return context;
		}

		public bool Same
			( SystemExecutionContext ctxToCompare
			)
		{
			if(rt.dynamic.ecsact_system_execution_context_same == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_same"
				);
			}

			return rt.dynamic.ecsact_system_execution_context_same(
				contextPtr,
				ctxToCompare.contextPtr
			);
		}

		public T Action<T>() where T : Ecsact.Action, new() {
			if(rt.dynamic.ecsact_system_execution_context_action == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_action"
				);
			}

			object actionData = new T();

			GCHandle handle = GCHandle.Alloc(actionData, GCHandleType.Pinned);
			IntPtr actionPtr = handle.AddrOfPinnedObject();

			try {
				rt.dynamic.ecsact_system_execution_context_action(
					contextPtr,
					actionPtr
				);
			} finally {
				handle.Free();
			}

			Type actionType = typeof(T);
			var actionObject = Marshal.PtrToStructure(actionPtr, actionType);
			var action = (T)actionObject;
			return action;
		}

		public void Update<C>
			( C component
			) where C : Ecsact.Component
		{
			if(rt.dynamic.ecsact_system_execution_context_update == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_update_component");
			}

			var componentId = Ecsact.Util.GetComponentID<C>();
			var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

			try {
				Marshal.StructureToPtr(component, componentPtr, false);
				rt.dynamic.ecsact_system_execution_context_update(
					contextPtr,
					componentId,
					componentPtr
				);
			} finally {
				Marshal.FreeHGlobal(componentPtr);
			}
		}

		public Int32 Entity() {
			if(rt.dynamic.ecsact_system_execution_context_entity == null) {
				throw new EcsactRuntimeMissingMethod(
					"ecsact_system_execution_context_entity"
				);
			}

			return rt.dynamic.ecsact_system_execution_context_entity(contextPtr);
		}

		private IntPtr contextPtr;
		private EcsactRuntime rt;

	}

	public delegate void CSystemExecutionImpl
		( IntPtr context
		);
	
	public delegate void SystemExecutionImpl
		( EcsactRuntime.SystemExecutionContext context
		);

	public delegate Int32 ActionCompareFn
		( IntPtr  firstAction
		, IntPtr  secondAction
		);

	public delegate Int32 ComponentCompareFn
		( IntPtr  firstComponent
		, IntPtr  secondComponent
		);

	public delegate void AsyncErrorCallback
		( Ecsact.AsyncError  err
		, Int32              requestId
		, IntPtr             callbackUserData
		);

	public delegate void AsyncConnectCallback
		( [MarshalAs(UnmanagedType.LPStr)] string  connectAddress
		, Int32                                    connectPort
		, IntPtr                                   callbackUserData
		);

	public delegate void  AsyncActionCommittedCallback
		( Int32   actionId
		, object  actionData
		, Int32   committedTick
		, Int32   requestId
		, IntPtr  callbackUserData
		);

	public struct AsyncEventsCollector {
		public AsyncErrorCallback errorCallback;
		public IntPtr errorCallbackUserData;
		public AsyncConnectCallback connectCallback;
		public IntPtr connectCallbackUserData;
		public AsyncActionCommittedCallback actionCommittedCallback;
		public IntPtr actionCommittedCallbackUserData;
	}

	static EcsactRuntime() {
#if UNITY_EDITOR
		EditorApplication.playModeStateChanged += state => {
			if(state == PlayModeStateChange.ExitingPlayMode) {
				EditorApplication.delayCall += () => {
					Ecsact.Internal.EcsactRuntimeDefaults.ClearDefaults();
				};
			}
		};
#endif
	}

	private IntPtr[]? _libs;
	private Core? _core;
	private Async? _async;
	private Dynamic? _dynamic;
	private Meta? _meta;
	private Serialize? _serialize;
	private Static? _static;
	private Wasm? _wasm;

	public class ModuleBase {
		internal List<string> _availableMethods = new();
		public IEnumerable<string> availableMethods => _availableMethods;

#if UNITY_EDITOR
		public static string[]? GetMethodsList<M>() where M : ModuleBase {
			var allMethodsPropertyInfo = typeof(M).GetProperty(
				"methods",
				BindingFlags.Static | BindingFlags.Public
			);
			string[]? allMethods = null;

			if(allMethodsPropertyInfo != null) {
				allMethods = allMethodsPropertyInfo.GetValue(null) as string[];
			}

			return allMethods;
		}
#endif
	}

	public class Async : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_async_connect",
			"ecsact_async_disconnect",
			"ecsact_async_execute_action_at",
			"ecsact_async_execute_action",
			"ecsact_async_flush_events",
		};

		internal delegate void ecsact_async_execute_action_delegate
			( Int32   actionId
			, IntPtr  actionData
			);
		internal ecsact_async_execute_action_delegate? ecsact_async_execute_action;

		internal delegate void ecsact_async_execute_action_at_delegate
			( Int32   actionId
			, IntPtr  actionData
			, Int32   tick
			);
		internal ecsact_async_execute_action_at_delegate? ecsact_async_execute_action_at;

		internal delegate void ecsact_async_flush_events_delegate
			( in ExecutionEventsCollector  executionEventsCollector
			, in AsyncEventsCollector      asyncEventsCollector
			);
		internal ecsact_async_flush_events_delegate? ecsact_async_flush_events;

		internal delegate void ecsact_async_connect_delegate
			( [MarshalAs(UnmanagedType.LPStr)] string connectionString
			);
		internal ecsact_async_connect_delegate? ecsact_async_connect;

		internal delegate void ecsact_async_disconnect_delegate
			(
			);
		internal ecsact_async_disconnect_delegate? ecsact_async_disconnect;

		public delegate void ErrorCallback
			( Ecsact.AsyncError  err
			, Int32              requestId
			);

		public delegate void ConnectCallback
			( string  connectAddress
			, Int32   connectPort
			);

		private AsyncEventsCollector _asyncEvs;
		private List<ErrorCallback> _errCallbacks = new();
		private List<ConnectCallback> _connectCallbacks = new();
		private EcsactRuntime _owner;

		internal Async
			( EcsactRuntime owner
			)
		{
			_owner = owner;
			_asyncEvs = new AsyncEventsCollector{
				actionCommittedCallback = OnActionCommittedHandler,
				actionCommittedCallbackUserData = IntPtr.Zero,
				errorCallback = OnErrorHandler,
				errorCallbackUserData = IntPtr.Zero,
				connectCallback = OnConnectHandler,
				connectCallbackUserData = IntPtr.Zero,
			};
		}

		[AOT.MonoPInvokeCallback(typeof(AsyncActionCommittedCallback))]
		private static void OnActionCommittedHandler
			( Int32   actionId
			, object  actionData
			, Int32   committedTick
			, Int32   requestId
			, IntPtr  callbackUserData
			)
		{
			var self = (GCHandle.FromIntPtr(callbackUserData).Target as Async)!;
		}

		[AOT.MonoPInvokeCallback(typeof(AsyncErrorCallback))]
		private static void OnErrorHandler
			( Ecsact.AsyncError  err
			, Int32              requestId
			, IntPtr             callbackUserData
			)
		{
			var self = (GCHandle.FromIntPtr(callbackUserData).Target as Async)!;
			foreach(var cb in self._errCallbacks) {
				cb(err, requestId);
			}
		}

		[AOT.MonoPInvokeCallback(typeof(AsyncConnectCallback))]
		private static void OnConnectHandler
			( [MarshalAs(UnmanagedType.LPStr)] string  connectAddress
			, Int32                                    connectPort
			, IntPtr                                   callbackUserData
			)
		{
			var self = (GCHandle.FromIntPtr(callbackUserData).Target as Async)!;
			foreach(var cb in self._connectCallbacks) {
				cb(connectAddress, connectPort);
			}
		}

		public Action OnError
			( ErrorCallback callback
			)
		{
			_errCallbacks.Add(callback);

			return () => {
				_errCallbacks.Remove(callback);
			};
		}

		public Action OnConnect
			( ConnectCallback callback
			)
		{
			_connectCallbacks.Add(callback);

			return () => {
				_connectCallbacks.Remove(callback);
			};
		}

		public void Connect
			( string connectionString
			)
		{
			if(ecsact_async_connect == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_async_connect");
			}
			ecsact_async_connect(connectionString);
		}

		public void FlushEvents() {
			if(ecsact_async_flush_events == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_async_flush_events");
			}

			var selfPinned = GCHandle.Alloc(this, GCHandleType.Pinned);
			var ownerPinned = GCHandle.Alloc(_owner, GCHandleType.Pinned);
			try {
				var selfIntPtr = GCHandle.ToIntPtr(selfPinned);
				var ownerIntPtr = GCHandle.ToIntPtr(ownerPinned);
				_owner._execEvs.initCallbackUserData = ownerIntPtr;
				_owner._execEvs.updateCallbackUserData = ownerIntPtr;
				_owner._execEvs.removeCallbackUserData = ownerIntPtr;
				_asyncEvs.errorCallbackUserData = selfIntPtr;
				_asyncEvs.connectCallbackUserData = selfIntPtr;
				_asyncEvs.actionCommittedCallbackUserData = selfIntPtr;
				ecsact_async_flush_events(in _owner._execEvs, in _asyncEvs);
			} finally {
				selfPinned.Free();
				ownerPinned.Free();
			}
		}
	}

	public class Core : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_add_component",
			"ecsact_clear_registry",
			"ecsact_count_components",
			"ecsact_count_entities",
			"ecsact_create_entity",
			"ecsact_create_registry",
			"ecsact_destroy_entity",
			"ecsact_destroy_registry",
			"ecsact_each_component",
			"ecsact_ensure_entity",
			"ecsact_entity_exists",
			"ecsact_execute_systems",
			"ecsact_get_component",
			"ecsact_get_components",
			"ecsact_get_entities",
			"ecsact_has_component",
			"ecsact_remove_component",
			"ecsact_update_component",
		};

		internal delegate Int32 ecsact_create_registry_delegate
			( string registryName
			);
		internal ecsact_create_registry_delegate? ecsact_create_registry;

		internal delegate void ecsact_destroy_registry_delegate
			( Int32 registryId
			);
		internal ecsact_destroy_registry_delegate? ecsact_destroy_registry;

		internal delegate void ecsact_clear_registry_delegate
			( Int32 registryId
			);
		internal ecsact_clear_registry_delegate? ecsact_clear_registry;

		internal delegate Int32 ecsact_create_entity_delegate
			( Int32 registryId
			);
		internal ecsact_create_entity_delegate? ecsact_create_entity;

		internal delegate void ecsact_ensure_entity_delegate
			( Int32 registryId
			, Int32 entityId
			);
		internal ecsact_ensure_entity_delegate? ecsact_ensure_entity;

		internal delegate bool ecsact_entity_exists_delegate
			( Int32 registryId
			, Int32 entityId
			);
		internal ecsact_entity_exists_delegate? ecsact_entity_exists;

		internal delegate void ecsact_destroy_entity_delegate
			( Int32 registryId
			, Int32 entityId
			);
		internal ecsact_destroy_entity_delegate? ecsact_destroy_entity;

		internal delegate Int32 ecsact_count_entities_delegate
			( Int32 registryId
			);
		internal ecsact_count_entities_delegate? ecsact_count_entities;

		internal delegate void ecsact_get_entities_delegate
			( Int32        registryId
			, Int32        maxEntitiesCount
			, out Int32[]  outEntities
			, out Int32    outEntitiesCount
			);
		internal ecsact_get_entities_delegate? ecsact_get_entities;

		internal enum ecsact_add_error {
			OK = 0,
			ENTITY_INVALID = 1,
			CONSTRAINT_BROKEN = 2
		};

		internal delegate ecsact_add_error ecsact_add_component_delegate
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			, IntPtr  componentData
			);
		internal ecsact_add_component_delegate? ecsact_add_component;

		internal delegate bool ecsact_has_component_delegate
			( Int32  registryId
			, Int32  entityId
			, Int32  componentId
			);
		internal ecsact_has_component_delegate? ecsact_has_component;

		internal delegate IntPtr ecsact_get_component_delegate
			( Int32  registryId
			, Int32  entityId
			, Int32  componentId
			);
		internal ecsact_get_component_delegate? ecsact_get_component;

		internal delegate void ecsact_each_component_delegate
			( Int32                  registryId
			, Int32                  entityId
			, EachComponentCallback  callback
			, IntPtr                 callbackUserData
			);
		internal ecsact_each_component_delegate? ecsact_each_component;

		internal delegate Int32 ecsact_count_components_delegate
			( Int32  registryId
			, Int32  entityId
			);
		internal ecsact_count_components_delegate? ecsact_count_components;

		
		internal delegate void ecsact_get_components_delegate
			( Int32         registryId
			, Int32         entityId
			, Int32         maxComponentsCount
			, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]
				Int32[]   outComponentIds
			, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]
				IntPtr[]  outComponentsData
			, out Int32     outComponentsCount
			);
		internal ecsact_get_components_delegate? ecsact_get_components;

		internal enum ecsact_update_error {
			OK = 0,
			ENTITY_INVALID = 1,
			CONSTRAINT_BROKEN = 2
		};

		internal delegate ecsact_update_error ecsact_update_component_delegate
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			, IntPtr  componentData
			);
		internal ecsact_update_component_delegate? ecsact_update_component;

		internal delegate void ecsact_remove_component_delegate
			( Int32  registryId
			, Int32  entityId
			, Int32  componentId
			);
		internal ecsact_remove_component_delegate? ecsact_remove_component;

		internal delegate void ecsact_component_event_callback
			( EcsactEvent  ev
			, Int32        entityId
			, Int32        componentId
			, object       componentData
			, IntPtr       callbackUserData
			);

		internal enum ecsact_exec_systems_error {
			OK = 0,
			ENTITY_INVALID = 1,
			CONSTRAINT_BROKEN = 2
		};

		internal delegate ecsact_exec_systems_error ecsact_execute_systems_delegate
			( Int32                        registryId
			, Int32                        executionCount
			, ExecutionOptions[]           executionOptionsList
			, in ExecutionEventsCollector  eventsCollector
			);
		internal ecsact_execute_systems_delegate? ecsact_execute_systems;

		private EcsactRuntime _owner;

		internal Core
			( EcsactRuntime owner
			)
		{
			_owner = owner;
		}

		// NOTE(Kelwan): Currently internal to keep the registry count to 1
		// Addressed in issue: https://github.com/ecsact-dev/ecsact_unity/issues/28
		internal Int32 CreateRegistry
			( string registryName
			)
		{
			if(ecsact_create_registry == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_create_registry");
			}

			return ecsact_create_registry(registryName);
		}

		public void DestroyRegistry
			( Int32 registryId
			)
		{
			if(ecsact_destroy_registry == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_destroy_registry");
			}

			ecsact_destroy_registry(registryId);
		}

		public void ClearRegistry
			( Int32 registryId
			)
		{
			if(ecsact_clear_registry == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_clear_registry");
			}

			ecsact_clear_registry(registryId);
		}

		public Int32 CreateEntity
			( Int32 registryId
			)
		{
			if(ecsact_create_entity == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_create_entity");
			}

			return ecsact_create_entity(registryId);
		}

		public void EnsureEntity
			( Int32 registryId
			, Int32 entityId
			)
		{
			if(ecsact_ensure_entity == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_ensure_entity");
			}

			ecsact_ensure_entity(registryId, entityId);
		}

		public bool EntityExists
			( Int32 registryId
			, Int32 entityId
			)
		{
			if(ecsact_entity_exists == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_entity_exists");
			}

			return ecsact_entity_exists(registryId, entityId);
		}

		public void DestroyEntity
			( Int32 registryId
			, Int32 entityId
			)
		{
			if(ecsact_destroy_entity == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_destroy_entity");
			}

			ecsact_destroy_entity(registryId, entityId);
		}

		public Int32 CountEntities
			( Int32 registryId
			)
		{
			if(ecsact_count_entities == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_count_entities");
			}

			return ecsact_count_entities(registryId);
		}

		public void GetEntities
			( Int32        registryId
			, Int32        maxEntitiesCount
			, out Int32[]  outEntities
			, out Int32    outEntitiesCount
			)
		{
			if(ecsact_get_entities == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_entities");
			}

			ecsact_get_entities(
				registryId,
				maxEntitiesCount,
				out outEntities,
				out outEntitiesCount
			);
		}

		public Int32[] GetEntities
			( Int32 registryId
			)
		{
			var entitiesCount = CountEntities(registryId);
			var entities = new Int32[entitiesCount];

			GetEntities(
				registryId,
				entitiesCount,
				out entities,
				out entitiesCount
			);

			return entities;
		}

		public void AddComponent<C>
			( Int32  registryId
			, Int32  entityId
			, C      component
			) where C : Ecsact.Component
		{
			if(ecsact_add_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_add_component");
			}

			var componentId = Ecsact.Util.GetComponentID<C>();

#if UNITY_EDITOR
			var result = HasComponent<C>(registryId, entityId);
			if(result == true) {
				throw new Exception("Entity already has added component");
			}
#endif
			var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

			try {
				Marshal.StructureToPtr(component, componentPtr, false);
				var error = ecsact_add_component(
					registryId,
					entityId,
					componentId,
					componentPtr
				);
				if(error == ecsact_add_error.ENTITY_INVALID) {
					throw new Exception("Component add happening on invalid entity");
				}
				if(error == ecsact_add_error.CONSTRAINT_BROKEN) {
					throw new Exception("Component add constraint broken");
				}
			} finally {
				Marshal.FreeHGlobal(componentPtr);
			}

			_owner._TriggerInitComponentEvent(entityId, componentId, component);
		}

		public void AddComponent
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			, object  componentData
			)
		{
			if(ecsact_add_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_add_component");
			}

#if UNITY_EDITOR
			var result = HasComponent(registryId, entityId, componentId);
			if(result == true) {
				throw new Exception("Entity already has added component");
			}
#endif
			var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(componentData));

			try {
				Marshal.StructureToPtr(componentData, componentPtr, false);
				var error = ecsact_add_component(
					registryId,
					entityId,
					componentId,
					componentPtr
				);
				if(error == ecsact_add_error.ENTITY_INVALID) {
					throw new Exception("Component add happening on invalid entity");
				}
				if(error == ecsact_add_error.CONSTRAINT_BROKEN) {
					throw new Exception("Component add constraint broken");
				}
			} finally {
				Marshal.FreeHGlobal(componentPtr);
			}

			_owner._TriggerInitComponentEvent(entityId, componentId, componentData);
		}

		public bool HasComponent
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			)
		{
			if(ecsact_has_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_has_component");
			}

			return ecsact_has_component(registryId, entityId, componentId);
		}

		public bool HasComponent<C>
			( Int32   registryId
			, Int32   entityId
			) where C :Ecsact.Component
		{
			if(ecsact_has_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_has_component");
			}

			var componentId = Ecsact.Util.GetComponentID<C>();

			return ecsact_has_component(registryId, entityId, componentId);
		}

		public C GetComponent<C>
			( Int32  registryId
			, Int32  entityId
			) where C : Ecsact.Component
		{
			if(ecsact_get_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_component");
			}
			var componentId = Ecsact.Util.GetComponentID<C>();

#if UNITY_EDITOR
			var result = HasComponent<C>(registryId, entityId);
			if(result == false) {
				throw new Exception("Can't get a component that doesn't exist");
			}
#endif

			var componentPtr = 
				ecsact_get_component(registryId, entityId, componentId);

			var componentObject =
					Ecsact.Util.PtrToComponent<C>(componentPtr);
			var component = (C)componentObject;
			return component;
		}

		public object GetComponent
			( Int32 registryId
			, Int32 entityId
			, Int32 componentId
			)
		{
			if(ecsact_get_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_component");
			}

			var componentPtr =
				ecsact_get_component(registryId, entityId, componentId);

			var componentObject =
					Ecsact.Util.PtrToComponent(componentPtr, componentId);

			componentObject = Ecsact.Util.HandlePtrToComponent(ref componentObject);

			return componentObject;
			
		}
	
		public Int32 CountComponents
			( Int32  registryId
			, Int32  entityId
			)
		{
			if(ecsact_count_components == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_count_components");
			}

			return ecsact_count_components(registryId, entityId);
		}

		public Dictionary<Int32, object> GetComponents
			( Int32  registryId
			, Int32  entityId
			)
		{
			if(ecsact_get_components == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_components");
			}
			// @Kelwan: Remove the +1 from count and the for loop below:
			// Link to issue: https://github.com/ecsact-dev/ecsact_rt_entt/issues/9
			Int32 count = CountComponents(registryId, entityId) + 1;

			Int32[] componentIds = new Int32[count];
			IntPtr[] componentsData = new IntPtr[count];
			Int32 componentCount;

			ecsact_get_components(
				registryId,
				entityId,
				count,
				componentIds,
				componentsData,
				out componentCount
			);
			
			Dictionary<Int32, object> componentObjects = 
				new Dictionary<Int32, object>();

			for(int i = 1; i < componentCount + 1; i++) {
				var componentObject = Ecsact.Util.PtrToComponent(
					componentsData[i],
					componentIds[i]
				);
				componentObject = Ecsact.Util.HandlePtrToComponent(ref componentObject);

				componentObjects.Add(
					componentIds[i],
					componentObject
				);
			}
			return componentObjects;
		}

		public void EachComponent
			( Int32                  registryId
			, Int32                  entityId
			, EachComponentCallback  callback
			, IntPtr                 callbackUserData
			)
		{
			if(ecsact_each_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_each_component");
			}

			ecsact_each_component(
				registryId,
				entityId,
				callback,
				callbackUserData
			);
		}

		public void UpdateComponent<C>
			( Int32  registryId
			, Int32  entityId
			, C      component
			) where C : Ecsact.Component
		{
			if(ecsact_update_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_update_component");
			}
			var componentId = Ecsact.Util.GetComponentID<C>();

#if UNITY_EDITOR
			var result = HasComponent<C>(registryId, entityId);
			if(result == false) {
				throw new Exception("Can't update a component that doesn't exist");
			}
#endif

			var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(component));

			try {
				Marshal.StructureToPtr(component, componentPtr, false);
				var error = ecsact_update_component(
				registryId,
				entityId,
				componentId,
				componentPtr
			);

			if(error == ecsact_update_error.ENTITY_INVALID) {
				throw new Exception("Component update happening on invalid entity");
			}
			if(error == ecsact_update_error.CONSTRAINT_BROKEN) {
				throw new Exception("Component update constraint broken");
			}
			} finally {
				Marshal.FreeHGlobal(componentPtr);
			}

		}

		public void RemoveComponent<C>
			( Int32  registryId
			, Int32  entityId
			) where C : Ecsact.Component
		{
			if(ecsact_remove_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_remove_component");
			}

#if UNITY_EDITOR
			var result = HasComponent<C>(registryId, entityId);
			if(result == false) {
				throw new Exception("Can't remove a component that doesn't exist");
			}
#endif

			var componentData = GetComponent<C>(registryId, entityId);
			var componentId = Ecsact.Util.GetComponentID<C>();
			ecsact_remove_component(registryId, entityId, componentId);
			_owner._TriggerRemoveComponentEvent(entityId, componentId, componentData);
		}

		public void RemoveComponent
			( Int32  registryId
			, Int32  entityId
			, Int32  componentId
			)
		{
			if(ecsact_remove_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_remove_component");
			}

#if UNITY_EDITOR
			var result = HasComponent(registryId, entityId, componentId);
			if(result == false) {
				throw new Exception("Can't remove a component that doesn't exist");
			}
#endif

			var componentData = GetComponent(registryId, entityId, componentId);
			ecsact_remove_component(registryId, entityId, componentId);
			_owner._TriggerRemoveComponentEvent(entityId, componentId, componentData);
		}

		public void ExecuteSystems
			( Int32               registryId
			, Int32               executionCount
			, ExecutionOptions[]  executionOptionsList
			)
		{
			if(ecsact_execute_systems == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_execute_systems");
			}

			CurrentSystemExecutionState.runtime = _owner;

			var ownerPinned = GCHandle.Alloc(_owner, GCHandleType.Pinned);

			try {
				var ownerIntPtr = GCHandle.ToIntPtr(ownerPinned);
				_owner._execEvs.initCallbackUserData = ownerIntPtr;
				_owner._execEvs.updateCallbackUserData = ownerIntPtr;
				_owner._execEvs.removeCallbackUserData = ownerIntPtr;
				var error = ecsact_execute_systems(
					registryId,
					executionCount,
					executionOptionsList,
					in _owner._execEvs
				);
				if(error == ecsact_exec_systems_error.ENTITY_INVALID) {
					throw new Exception("System execution happening on invalid entity");
				}
				if(error == ecsact_exec_systems_error.CONSTRAINT_BROKEN) {
					throw new Exception("System execution constraint broken");
				}
			} finally {
				ownerPinned.Free();
				CurrentSystemExecutionState.runtime = null;
			}
		}
	}

	public class Dynamic : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_add_child_system",
			"ecsact_add_dependency",
			"ecsact_add_enum_value",
			"ecsact_add_field",
			"ecsact_add_system_generates",
			"ecsact_create_action",
			"ecsact_create_component",
			"ecsact_create_enum",
			"ecsact_create_package",
			"ecsact_create_system",
			"ecsact_create_transient",
			"ecsact_destroy_component",
			"ecsact_destroy_enum",
			"ecsact_destroy_package",
			"ecsact_destroy_transient",
			"ecsact_remove_child_system",
			"ecsact_remove_dependency",
			"ecsact_remove_enum_value",
			"ecsact_remove_field",
			"ecsact_remove_system_generates",
			"ecsact_reorder_system",
			"ecsact_set_package_source_file_path",
			"ecsact_set_system_association_capability",
			"ecsact_set_system_capability",
			"ecsact_set_system_execution_impl",
			"ecsact_system_execution_context_action",
			"ecsact_system_execution_context_add",
			"ecsact_system_execution_context_generate",
			"ecsact_system_execution_context_get",
			"ecsact_system_execution_context_has",
			"ecsact_system_execution_context_id",
			"ecsact_system_execution_context_other",
			"ecsact_system_execution_context_parent",
			"ecsact_system_execution_context_remove",
			"ecsact_system_execution_context_same",
			"ecsact_system_execution_context_update",
			"ecsact_system_execution_context_entity",
			"ecsact_system_generates_set_component",
			"ecsact_system_generates_unset_component",
			"ecsact_unset_system_association_capability",
			"ecsact_unset_system_capability",
		};

		internal delegate void ecsact_system_execution_context_action_delegate
			( IntPtr  context
			, IntPtr  outActionData
			);
		internal ecsact_system_execution_context_action_delegate? ecsact_system_execution_context_action;

		internal delegate void ecsact_system_execution_context_add_delegate
			( IntPtr  context
			, Int32   componentId
			, IntPtr  componentData
			);
		internal ecsact_system_execution_context_add_delegate? ecsact_system_execution_context_add;

		internal delegate void ecsact_system_execution_context_remove_delegate
			( IntPtr  context
			, Int32   componentId
			);
		internal ecsact_system_execution_context_remove_delegate? ecsact_system_execution_context_remove;

		internal delegate void ecsact_system_execution_context_get_delegate
			( IntPtr  context
			, Int32   componentId
			, IntPtr  outComponentData
			);
		internal ecsact_system_execution_context_get_delegate? ecsact_system_execution_context_get;

		internal delegate void ecsact_system_execution_context_update_delegate
			( IntPtr  context
			, Int32   componentId
			, IntPtr  outComponentData
			);
		internal ecsact_system_execution_context_update_delegate? ecsact_system_execution_context_update;

		internal delegate bool ecsact_system_execution_context_has_delegate
			( IntPtr  context
			, Int32   componentId
			);
		internal ecsact_system_execution_context_has_delegate? ecsact_system_execution_context_has;

		internal delegate void ecsact_system_execution_context_generate_delegate
			( IntPtr    context
			, Int32     componentCount
			, Int32[]   componentIds
			, IntPtr[]  componentsData
			);
		internal ecsact_system_execution_context_generate_delegate? ecsact_system_execution_context_generate;

		internal delegate IntPtr ecsact_system_execution_context_parent_delegate
			( IntPtr context
			);
		internal ecsact_system_execution_context_parent_delegate? ecsact_system_execution_context_parent;

		internal delegate bool ecsact_system_execution_context_same_delegate
			( IntPtr  firstContext
			, IntPtr  secondContext
			);
		internal ecsact_system_execution_context_same_delegate? ecsact_system_execution_context_same;

		internal delegate void ecsact_create_system_delegate
			( [MarshalAs(UnmanagedType.LPStr)] string  systemName
			, Int32                                    parentSystemId
			, Int32[]                                  capabilityComponentIds
			, SystemCapability[]                       capabilities
			, Int32                                    capabilitiesCount
			, SystemExecutionImpl                      executionImpl
			);
		internal ecsact_create_system_delegate? ecsact_create_system;


		internal delegate Int32 ecsact_system_execution_context_entity_delegate
			( IntPtr context
			);
		internal ecsact_system_execution_context_entity_delegate? ecsact_system_execution_context_entity;


		internal delegate void ecsact_set_system_execution_impl_delegate
			( Int32                systemId
			, CSystemExecutionImpl  executionImpl
			);
		internal ecsact_set_system_execution_impl_delegate? ecsact_set_system_execution_impl;

		internal delegate void ecsact_create_action_delegate
			( [MarshalAs(UnmanagedType.LPStr)] string  actionName
			, Int32                                    actionSize
			, ActionCompareFn                          actionCompareFn
			, Int32[]                                  capabilityComponentIds
			, SystemCapability[]                       capabilities
			, Int32                                    capabilitiesCount
			, CSystemExecutionImpl                     executionImpl
			);
		internal ecsact_create_action_delegate? ecsact_create_action;

		internal delegate void ecsact_create_component_delegate
			( [MarshalAs(UnmanagedType.LPStr)] string  componentName
			, Int32                                    componentSize
			, ComponentCompareFn                       componentCompareFn
			);
		internal ecsact_create_component_delegate? ecsact_create_component;

		internal delegate void ecsact_destroy_component_delegate
			( Int32 componentId
			);
		internal ecsact_destroy_component_delegate? ecsact_destroy_component;

		internal delegate Int32 ecsact_system_execution_context_id_delegate
			( IntPtr context
			);
		internal ecsact_system_execution_context_id_delegate? ecsact_system_execution_context_id;

		internal delegate Int32 ecsact_system_execution_context_other_delegate
			( IntPtr  context
			, Int32   entityId
			);
		internal ecsact_system_execution_context_other_delegate? ecsact_system_execution_context_other;

		public void SetSystemExecutionImpl
			( Int32                systemId
			, SystemExecutionImpl  executionImpl
			)
		{
			if(ecsact_set_system_execution_impl == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_set_system_execution_impl");
			}

			_system_impls.Add(systemId, executionImpl);

			ecsact_set_system_execution_impl(systemId, CExecutionImpl);
		}

		public void SetSystemExecutionImpl<System>
			( SystemExecutionImpl executionImpl
			) where System : Ecsact.System
		{
			if(ecsact_set_system_execution_impl == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_set_system_execution_impl");
			}

			var systemId = Ecsact.Util.GetSystemID<System>();
			_system_impls.Add(systemId, executionImpl);

			ecsact_set_system_execution_impl(systemId, CExecutionImpl);
		}

		public void SetActionExecutionImpl<Action>
			( SystemExecutionImpl executionImpl
			) where Action : Ecsact.Action
		{
			if(ecsact_set_system_execution_impl == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_set_system_execution_impl");
			}

			var systemId = Ecsact.Util.GetActionID<Action>();
			_system_impls.Add(systemId, executionImpl);

			ecsact_set_system_execution_impl(systemId, CExecutionImpl);	
		}

		static void CExecutionImpl
			( IntPtr contextPtr
			)
		{
			var rt = CurrentSystemExecutionState.runtime;
			if(rt is null) {
				throw new Exception(
					"SystemExecutionContext can only be used in a system implementation"
				);
			}

			var sysExecCtx = new SystemExecutionContext(contextPtr);

			rt._dynamic!._system_impls[sysExecCtx.ID()](sysExecCtx);
		}

		internal Dictionary<Int32, SystemExecutionImpl> _system_impls = 
			new Dictionary<Int32, SystemExecutionImpl>();
	
	}

	public class Meta : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_meta_action_name",
			"ecsact_meta_component_name",
			"ecsact_meta_count_actions",
			"ecsact_meta_count_child_systems",
			"ecsact_meta_count_components",
			"ecsact_meta_count_dependencies",
			"ecsact_meta_count_enum_values",
			"ecsact_meta_count_enums",
			"ecsact_meta_count_fields",
			"ecsact_meta_count_packages",
			"ecsact_meta_count_system_generates_components",
			"ecsact_meta_count_system_generates_ids",
			"ecsact_meta_count_systems",
			"ecsact_meta_count_top_level_systems",
			"ecsact_meta_count_transients",
			"ecsact_meta_decl_full_name",
			"ecsact_meta_enum_name",
			"ecsact_meta_enum_storage_type",
			"ecsact_meta_enum_value_name",
			"ecsact_meta_enum_value",
			"ecsact_meta_field_name",
			"ecsact_meta_field_type",
			"ecsact_meta_get_action_ids",
			"ecsact_meta_get_child_system_ids",
			"ecsact_meta_get_component_ids",
			"ecsact_meta_get_dependencies",
			"ecsact_meta_get_enum_ids",
			"ecsact_meta_get_enum_value_ids",
			"ecsact_meta_get_field_ids",
			"ecsact_meta_get_package_ids",
			"ecsact_meta_get_parent_system_id",
			"ecsact_meta_get_system_ids",
			"ecsact_meta_get_top_level_systems",
			"ecsact_meta_get_transient_ids",
			"ecsact_meta_main_package",
			"ecsact_meta_package_file_path",
			"ecsact_meta_package_name",
			"ecsact_meta_registry_name",
			"ecsact_meta_system_association_capabilities_count",
			"ecsact_meta_system_association_capabilities",
			"ecsact_meta_system_association_fields_count",
			"ecsact_meta_system_association_fields",
			"ecsact_meta_system_capabilities_count",
			"ecsact_meta_system_capabilities",
			"ecsact_meta_system_generates_components",
			"ecsact_meta_system_generates_ids",
			"ecsact_meta_system_name",
			"ecsact_meta_transient_name",
		};

		[return: MarshalAs(UnmanagedType.LPStr)]
		internal delegate string ecsact_meta_registry_name_delegate
			( Int32 registryId
			);
		internal ecsact_meta_registry_name_delegate? ecsact_meta_registry_name;

		[return: MarshalAs(UnmanagedType.LPStr)]
		internal delegate string ecsact_meta_component_name_delegate
			( Int32 componentId
			);
		internal ecsact_meta_component_name_delegate? ecsact_meta_component_name;

		[return: MarshalAs(UnmanagedType.LPStr)]
		internal delegate string ecsact_meta_action_name_delegate
			( Int32 actionId
			);
		internal ecsact_meta_action_name_delegate? ecsact_meta_action_name;

		[return: MarshalAs(UnmanagedType.LPStr)]
		internal delegate string ecsact_meta_system_name_delegate
			( Int32 systemId
			);
		internal ecsact_meta_system_name_delegate? ecsact_meta_system_name;

		internal delegate Int32 ecsact_meta_system_capabilities_count_delegate
			( Int32 systemId
			);
		internal ecsact_meta_system_capabilities_count_delegate? ecsact_meta_system_capabilities_count;

		internal delegate void ecsact_meta_system_capabilities_delegate
			( Int32                   systemId
			, out Int32[]             outCapabilityComponentIds
			, out SystemCapability[]  outCapabilities
			);
		internal ecsact_meta_system_capabilities_delegate? ecsact_meta_system_capabilities;
	}

	public class Serialize : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_deserialize_action",
			"ecsact_deserialize_component",
			"ecsact_serialize_action_size",
			"ecsact_serialize_action",
			"ecsact_serialize_component_size",
			"ecsact_serialize_component",
		};

		internal delegate Int32 ecsact_serialize_action_size_delegate
			( Int32 actionId
			);
		internal ecsact_serialize_action_size_delegate? ecsact_serialize_action_size;

		internal delegate Int32 ecsact_serialize_component_size_delegate
			( Int32 componentId
			);
		internal ecsact_serialize_component_size_delegate? ecsact_serialize_component_size;

		internal delegate void ecsact_serialize_action_delegate
			( Int32   actionId
			, IntPtr  actionData
			, IntPtr  outBytes
			);
		internal ecsact_serialize_action_delegate? ecsact_serialize_action;

		internal delegate void ecsact_serialize_component_delegate
			( Int32   componentId
			, object  inComponentData
			, IntPtr  outBytes
			);
		internal ecsact_serialize_component_delegate? ecsact_serialize_component;

		internal delegate void ecsact_deserialize_action_delegate
			( Int32   actionId
			, IntPtr  inBytes
			, IntPtr  outActionData
			);
		internal ecsact_deserialize_action_delegate? ecsact_deserialize_action;

		internal delegate void ecsact_deserialize_component_delegate
			( Int32       componentId
			, IntPtr      inBytes
			, out object  outComponentData
			);
		internal ecsact_deserialize_component_delegate? ecsact_deserialize_component;

		public void DeserializeAction
			( Int32   actionId
			, IntPtr  inBytes
			, IntPtr  outActionData
			)
		{
			if(ecsact_deserialize_action == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_deserialize_action");
			}

			ecsact_deserialize_action(actionId, inBytes, outActionData);
		}

		public void DeserializeComponent
			( Int32       componentId
			, IntPtr      inBytes
			, out object  outComponentData
			)
		{
			if(ecsact_deserialize_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_deserialize_component");
			}

			ecsact_deserialize_component(componentId, inBytes, out outComponentData);
		}

		public void SerializeActionSize
			( Int32 actionId
			)
		{
			if(ecsact_serialize_action_size == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_serialize_action_size");
			}

			ecsact_serialize_action_size(actionId);
		}

		public void SerializeAction
			( Int32   actionId
			, IntPtr  actionData
			, IntPtr  outBytes
			)
		{
			if(ecsact_serialize_action == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_serialize_action");
			}

			ecsact_serialize_action(actionId, actionData, outBytes);
		}

		public void SerializeComponentSize
			( Int32 componentId
			)
		{
			if(ecsact_serialize_component_size == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_serialize_component_size");
			}

			ecsact_serialize_component_size(componentId);
		}

		public void SerializeComponent
			( Int32   componentId
			, object  inComponentData
			, IntPtr  outBytes
			)
		{
			if(ecsact_serialize_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_serialize_component");
			}

			ecsact_serialize_component(componentId, inComponentData, outBytes);
		}
	}

	public class Static : ModuleBase {
		public static string[] methods => new string[]{
			"ecsact_static_actions",
			"ecsact_static_components",
			"ecsact_static_off_reload",
			"ecsact_static_on_reload",
			"ecsact_static_systems",
			"ecsact_static_variants",
		};

		internal delegate void ecsact_static_components_delegate
			( out StaticComponentInfo[]  outComponents
			, out Int32                  outComponentsCount
			);
		internal ecsact_static_components_delegate? ecsact_static_components;

		internal delegate void ecsact_static_systems_delegate
			( out StaticSystemInfo[]  outSystems
			, out Int32               outSystemsCount
			);
		internal ecsact_static_systems_delegate? ecsact_static_systems;

		internal delegate void ecsact_static_actions_delegate
			( out StaticActionInfo[]  outActions
			, out Int32               outActionsCount
			);
		internal ecsact_static_actions_delegate? ecsact_static_actions;

		internal delegate void ecsact_static_on_reload_delegate
			( StaticReloadCallback  callback
			, IntPtr                callbackUserData
			);
		internal ecsact_static_on_reload_delegate? ecsact_static_on_reload;

		internal delegate void ecsact_static_off_reload_delegate
			( StaticReloadCallback callback
			);
		internal ecsact_static_off_reload_delegate? ecsact_static_off_reload;
	}

	// TODO(zaucy): This is misplaced here. It should be organized somewhere else
	//              maybe in another package
	// ecsactsi_* fns
	public class Wasm : ModuleBase {
		public enum Error {
			Ok,
			OpenFail,
			ReadFail,
			CompileFail,
			InstantiateFail,
			ExportNotFound,
			ExportInvalid,
			GuestImportUnknown,
		}

		public static string[] methods => new string[]{
			"ecsactsi_wasm_load",
			"ecsactsi_wasm_load_file",
			"ecsactsi_wasm_set_trap_handler",
		};

		internal delegate Error ecsactsi_wasm_load_delegate
			( sbyte[]   wasmData
			, Int32     wasmDataSize
			, Int32     systemsCount
			, Int32[]   systmIds
			, string[]  wasmExports
			);

		internal ecsactsi_wasm_load_delegate? ecsactsi_wasm_load;

		internal delegate Error ecsactsi_wasm_load_file_delegate
			( [MarshalAs(UnmanagedType.LPStr)] string    wasmFilePath
			, Int32                                      systemsCount
			, Int32[]                                    systmIds
			, string[]                                   wasmExports
			);

		internal ecsactsi_wasm_load_file_delegate? ecsactsi_wasm_load_file;

		internal delegate void ecsactsi_wasm_trap_handler
			( Int32                                    systemId
			, [MarshalAs(UnmanagedType.LPStr)] string  trapMessage
			);

		internal delegate void ecsactsi_wasm_set_trap_handler_delegate
			( ecsactsi_wasm_trap_handler handler
			);

		internal ecsactsi_wasm_set_trap_handler_delegate? ecsactsi_wasm_set_trap_handler;

		public Error Load
			( byte[]  wasmData
			, Int32   systemId
			, string  exportName
			)
		{
			if(ecsactsi_wasm_load == null) {
				throw new EcsactRuntimeMissingMethod("ecsactsi_wasm_load");
			}

			var systemIds = new Int32[]{systemId};
			var exportNames = new string[]{exportName};

			return ecsactsi_wasm_load(
				(sbyte[]) (Array) wasmData,
				wasmData.Length,
				1,
				systemIds,
				exportNames
			);
		}
	}

	public Core core => _core!;
	public Async async => _async!;
	public Dynamic dynamic => _dynamic!;
	public Meta meta => _meta!;
	public Serialize serialize => _serialize!;
	public Static @static => _static!;
	public Wasm wasm => _wasm!;

	[AOT.MonoPInvokeCallback(typeof(Wasm.ecsactsi_wasm_trap_handler))]
	private static void DefaultWasmTrapHandler
		( Int32                                    systemId
		, [MarshalAs(UnmanagedType.LPStr)] string  trapMessage
		)
	{
		UnityEngine.Debug.LogError(
			$"[Wasm Trap (systemId={systemId})] {trapMessage}"
		);
	}

	private static void LoadDelegate<D, M>
		( IntPtr  lib
		, string  name
		, out D?  outDelegate
		, M       moduleInstance
		) where D : Delegate where M : ModuleBase
	{
#if UNITY_EDITOR
		var allMethods = ModuleBase.GetMethodsList<M>();

		if(allMethods != null) {
			if(!allMethods.Contains(name)) {
				UnityEngine.Debug.LogWarning(
					$"Missing {name} in {typeof(M).FullName} methods list"
				);
			}
		} else {
			UnityEngine.Debug.LogWarning(
				$"Missing methods list for {typeof(M).FullName}"
			);
		}
#endif

		IntPtr addr;
		if(NativeLibrary.TryGetExport(lib, name, out addr)) {
			outDelegate = Marshal.GetDelegateForFunctionPointer<D>(addr);
			if(outDelegate != null) {
				moduleInstance._availableMethods.Add(name);
			} else {
				UnityEngine.Debug.LogError(
					$"{name} is not a function in runtime library"
				);
			}
		} else {
			outDelegate = null;
		}
	}

	public static EcsactRuntime Load
		( IEnumerable<string> libraryPaths
		)
	{
		var runtime = new EcsactRuntime();
		runtime._core = new Core(runtime);
		runtime._async = new Async(runtime);
		runtime._dynamic = new Dynamic();
		runtime._meta = new Meta();
		runtime._serialize = new Serialize();
		runtime._static = new Static();
		runtime._wasm = new Wasm();
		runtime._libs =
			libraryPaths.Select(path => NativeLibrary.Load(path)).ToArray();

		foreach(var lib in runtime._libs) {
			// Load async methods
			LoadDelegate(lib, "ecsact_async_execute_action", out runtime._async.ecsact_async_execute_action, runtime._async);
			LoadDelegate(lib, "ecsact_async_execute_action_at", out runtime._async.ecsact_async_execute_action_at, runtime._async);
			LoadDelegate(lib, "ecsact_async_flush_events", out runtime._async.ecsact_async_flush_events, runtime._async);
			LoadDelegate(lib, "ecsact_async_connect", out runtime._async.ecsact_async_connect, runtime._async);
			LoadDelegate(lib, "ecsact_async_disconnect", out runtime._async.ecsact_async_disconnect, runtime._async);

			// Load core methods
			LoadDelegate(lib, "ecsact_create_registry", out runtime._core.ecsact_create_registry, runtime._core);
			LoadDelegate(lib, "ecsact_destroy_registry", out runtime._core.ecsact_destroy_registry, runtime._core);
			LoadDelegate(lib, "ecsact_clear_registry", out runtime._core.ecsact_clear_registry, runtime._core);
			LoadDelegate(lib, "ecsact_create_entity", out runtime._core.ecsact_create_entity, runtime._core);
			LoadDelegate(lib, "ecsact_ensure_entity", out runtime._core.ecsact_ensure_entity, runtime._core);
			LoadDelegate(lib, "ecsact_entity_exists", out runtime._core.ecsact_entity_exists, runtime._core);
			LoadDelegate(lib, "ecsact_destroy_entity", out runtime._core.ecsact_destroy_entity, runtime._core);
			LoadDelegate(lib, "ecsact_count_entities", out runtime._core.ecsact_count_entities, runtime._core);
			LoadDelegate(lib, "ecsact_get_entities", out runtime._core.ecsact_get_entities, runtime._core);
			LoadDelegate(lib, "ecsact_add_component", out runtime._core.ecsact_add_component, runtime._core);
			LoadDelegate(lib, "ecsact_has_component", out runtime._core.ecsact_has_component, runtime._core);
			LoadDelegate(lib, "ecsact_get_component", out runtime._core.ecsact_get_component, runtime._core);
			LoadDelegate(lib, "ecsact_each_component", out runtime._core.ecsact_each_component, runtime._core);
			LoadDelegate(lib, "ecsact_count_components", out runtime._core.ecsact_count_components, runtime._core);
			LoadDelegate(lib, "ecsact_get_components", out runtime._core.ecsact_get_components, runtime._core);
			LoadDelegate(lib, "ecsact_update_component", out runtime._core.ecsact_update_component, runtime._core);
			LoadDelegate(lib, "ecsact_remove_component", out runtime._core.ecsact_remove_component, runtime._core);
			LoadDelegate(lib, "ecsact_execute_systems", out runtime._core.ecsact_execute_systems, runtime._core);

			// Load dynamic methods
			LoadDelegate(lib, "ecsact_system_execution_context_action", out runtime._dynamic.ecsact_system_execution_context_action, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_add", out runtime._dynamic.ecsact_system_execution_context_add, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_remove", out runtime._dynamic.ecsact_system_execution_context_remove, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_update", out runtime._dynamic.ecsact_system_execution_context_update, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_get", out runtime._dynamic.ecsact_system_execution_context_get, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_has", out runtime._dynamic.ecsact_system_execution_context_has, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_generate", out runtime._dynamic.ecsact_system_execution_context_generate, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_parent", out runtime._dynamic.ecsact_system_execution_context_parent, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_same", out runtime._dynamic.ecsact_system_execution_context_same, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_entity", out runtime._dynamic.ecsact_system_execution_context_entity, runtime._dynamic);
			LoadDelegate(lib, "ecsact_create_system", out runtime._dynamic.ecsact_create_system, runtime._dynamic);
			LoadDelegate(lib, "ecsact_set_system_execution_impl", out runtime._dynamic.ecsact_set_system_execution_impl, runtime._dynamic);
			LoadDelegate(lib, "ecsact_create_action", out runtime._dynamic.ecsact_create_action, runtime._dynamic);
			LoadDelegate(lib, "ecsact_create_component", out runtime._dynamic.ecsact_create_component, runtime._dynamic);
			LoadDelegate(lib, "ecsact_destroy_component", out runtime._dynamic.ecsact_destroy_component, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_id", out runtime._dynamic.ecsact_system_execution_context_id, runtime._dynamic);
			LoadDelegate(lib, "ecsact_system_execution_context_other", out runtime._dynamic.ecsact_system_execution_context_other, runtime._dynamic);

			// Load meta methods
			LoadDelegate(lib, "ecsact_meta_registry_name", out runtime._meta.ecsact_meta_registry_name, runtime._meta);
			LoadDelegate(lib, "ecsact_meta_component_name", out runtime._meta.ecsact_meta_component_name, runtime._meta);
			LoadDelegate(lib, "ecsact_meta_action_name", out runtime._meta.ecsact_meta_action_name, runtime._meta);
			LoadDelegate(lib, "ecsact_meta_system_name", out runtime._meta.ecsact_meta_system_name, runtime._meta);
			LoadDelegate(lib, "ecsact_meta_system_capabilities_count", out runtime._meta.ecsact_meta_system_capabilities_count, runtime._meta);
			LoadDelegate(lib, "ecsact_meta_system_capabilities", out runtime._meta.ecsact_meta_system_capabilities, runtime._meta);

			// Load serialize methods
			LoadDelegate(lib, "ecsact_serialize_action_size", out runtime._serialize.ecsact_serialize_action_size, runtime._serialize);
			LoadDelegate(lib, "ecsact_serialize_component_size", out runtime._serialize.ecsact_serialize_component_size, runtime._serialize);
			LoadDelegate(lib, "ecsact_serialize_action", out runtime._serialize.ecsact_serialize_action, runtime._serialize);
			LoadDelegate(lib, "ecsact_serialize_component", out runtime._serialize.ecsact_serialize_component, runtime._serialize);
			LoadDelegate(lib, "ecsact_deserialize_action", out runtime._serialize.ecsact_deserialize_action, runtime._serialize);
			LoadDelegate(lib, "ecsact_deserialize_component", out runtime._serialize.ecsact_deserialize_component, runtime._serialize);

			// Load static methods
			LoadDelegate(lib, "ecsact_static_components", out runtime._static.ecsact_static_components, runtime._static);
			LoadDelegate(lib, "ecsact_static_systems", out runtime._static.ecsact_static_systems, runtime._static);
			LoadDelegate(lib, "ecsact_static_actions", out runtime._static.ecsact_static_actions, runtime._static);
			LoadDelegate(lib, "ecsact_static_on_reload", out runtime._static.ecsact_static_on_reload, runtime._static);
			LoadDelegate(lib, "ecsact_static_off_reload", out runtime._static.ecsact_static_off_reload, runtime._static);

			// Load system implementation wasm methods
			LoadDelegate(lib, "ecsactsi_wasm_load", out runtime._wasm.ecsactsi_wasm_load, runtime._wasm);
			LoadDelegate(lib, "ecsactsi_wasm_load_file", out runtime._wasm.ecsactsi_wasm_load_file, runtime._wasm);
			LoadDelegate(lib, "ecsactsi_wasm_set_trap_handler", out runtime._wasm.ecsactsi_wasm_set_trap_handler, runtime._wasm);
		}

		if(runtime._wasm.ecsactsi_wasm_set_trap_handler != null) {
			runtime._wasm.ecsactsi_wasm_set_trap_handler(DefaultWasmTrapHandler);
		}

		return runtime;
	}

	public static void Free
		( EcsactRuntime runtime
		)
	{
		if(runtime._libs != null) {
			foreach(var lib in runtime._libs) {
				NativeLibrary.Free(lib);
			}

			runtime._libs = null;
		}
	}

	/// <summary>Init Component Untyped Callback</summary>
	private delegate void InitCompUtCb
		( Int32   entityId
		, object  component
		);

	/// <summary>Update Component Untyped Callback</summary>
	private delegate void UpCompUtCb
		( Int32   entityId
		, object  component
		);

	/// <summary>Remove Component Untyped Callback</summary>
	private delegate void RmvCompUtCb
		( Int32   entityId
		, object  component
		);

	private List<InitComponentCallback> _initAnyCompCbs = new();
	private List<UpdateComponentCallback> _updateAnyCompCbs = new();
	private List<RemoveComponentCallback> _removeAnyCompCbs = new();
	private Dictionary<Int32, List<InitCompUtCb>> _initCompCbs = new();
	private Dictionary<Int32, List<UpCompUtCb>> _updateCompCbs = new();
	private Dictionary<Int32, List<RmvCompUtCb>> _removeCompCbs = new();
	internal ExecutionEventsCollector _execEvs;

	private EcsactRuntime() {
		_execEvs = new ExecutionEventsCollector{
			initCallback = OnInitComponentHandler,
			initCallbackUserData = IntPtr.Zero,
			updateCallback = OnUpdateComponentHandler,
			updateCallbackUserData = IntPtr.Zero,
			removeCallback = OnRemoveComponentHandler,
			removeCallbackUserData = IntPtr.Zero,
		};
	}

	~EcsactRuntime() {
		Free(this);
	}

	public delegate void InitComponentCallback
		( Int32   entityId
		, Int32   componentId
		, object  component
		);

	public Action OnInitComponent
		( InitComponentCallback callback
		)
	{
		_initAnyCompCbs.Add(callback);
		return () => {
			_initAnyCompCbs.Remove(callback);
		};
	}

	public delegate void InitComponentCallback<ComponentT>
		( Int32       entityId
		, ComponentT  component
		) where ComponentT : Ecsact.Component;

	/// <summary>Adds a callback for when component init event is fired.</summary>
	/// <returns>Action that clears callback upon invocation.</returns>
	public Action OnInitComponent<ComponentT>
		( InitComponentCallback<ComponentT> callback
		) where ComponentT : Ecsact.Component
	{
		var compId = Ecsact.Util.GetComponentID<ComponentT>()!;

		if(!_initCompCbs.TryGetValue(compId, out var callbacks)) {
			callbacks = new();
			_initCompCbs.Add(compId, callbacks);
		}

		InitCompUtCb cb = (entityId, comp) => {
			callback(entityId, (ComponentT)comp);
		};

		callbacks.Add(cb);

		return () => {
			if(_initCompCbs.TryGetValue(compId, out var callbacks)) {
				callbacks.Remove(cb);
			}
		};
	}

	public delegate void UpdateComponentCallback
		( Int32   entityId
		, Int32   componentId
		, object  component
		);

	public Action OnUpdateComponent
		( UpdateComponentCallback callback
		)
	{
		_updateAnyCompCbs.Add(callback);
		return () => {
			_updateAnyCompCbs.Remove(callback);
		};
	}

	public delegate void UpdateComponentCallback<ComponentT>
		( Int32       entityId
		, ComponentT  component
		) where ComponentT : Ecsact.Component;

	public Action OnUpdateComponent<ComponentT>
		( UpdateComponentCallback<ComponentT> callback
		) where ComponentT : Ecsact.Component
	{
		var compId = Ecsact.Util.GetComponentID<ComponentT>()!;

		if(!_updateCompCbs.TryGetValue(compId, out var callbacks)) {
			callbacks = new();
			_updateCompCbs.Add(compId, callbacks);
		}

		UpCompUtCb cb = (entityId, comp) => {
			callback(entityId, (ComponentT)comp);
		};

		callbacks.Add(cb);

		return () => {
			if(_updateCompCbs.TryGetValue(compId, out var callbacks)) {
				callbacks.Remove(cb);
			}
		};
	}

	public delegate void RemoveComponentCallback
		( Int32   entityId
		, Int32   componentId
		, object  component
		);

	public Action OnRemoveComponent
		( RemoveComponentCallback callback
		)
	{
		_removeAnyCompCbs.Add(callback);
		return () => {
			_removeAnyCompCbs.Remove(callback);
		};
	}

	public delegate void RemoveComponentCallback<ComponentT>
		( Int32       entityId
		, ComponentT  component
		) where ComponentT : Ecsact.Component;

	public Action OnRemoveComponent<ComponentT>
		( RemoveComponentCallback<ComponentT> callback
		) where ComponentT : Ecsact.Component
	{
		var compId = Ecsact.Util.GetComponentID<ComponentT>()!;

		if(!_removeCompCbs.TryGetValue(compId, out var callbacks)) {
			callbacks = new();
			_removeCompCbs.Add(compId, callbacks);
		}

		RmvCompUtCb cb = (entityId, comp) => {
			callback(entityId, (ComponentT)comp);
		};

		callbacks.Add(cb);

		return () => {
			if(_removeCompCbs.TryGetValue(compId, out var callbacks)) {
				callbacks.Remove(cb);
			}
		};
	}

	private void _TriggerInitComponentEvent
		( Int32   entityId
		, Int32   componentId
		, object  componentData
		)
	{
		if(_initCompCbs.TryGetValue(componentId, out var cbs)) {
			foreach(var cb in cbs) {
				cb(entityId, componentData);
			}
		}

		foreach(var cb in _initAnyCompCbs) {
			cb(entityId, componentId, componentData);
		}
	}

	private void _TriggerUpdateComponentEvent
		( Int32   entityId
		, Int32   componentId
		, object  componentData
		)
	{
		if(_updateCompCbs.TryGetValue(componentId, out var cbs)) {
			foreach(var cb in cbs) {
				cb(entityId, componentData);
			}
		}

		foreach(var cb in _updateAnyCompCbs) {
			cb(entityId, componentId, componentData);
		}
	}

	private void _TriggerRemoveComponentEvent
		( Int32   entityId
		, Int32   componentId
		, object  componentData
		)
	{
		if(_removeCompCbs.TryGetValue(componentId, out var cbs)) {
			foreach(var cb in cbs) {
				cb(entityId, componentData);
			}
		}

		foreach(var cb in _removeAnyCompCbs) {
			cb(entityId, componentId, componentData);
		}
	}

	[AOT.MonoPInvokeCallback(typeof(ComponentEventCallback))]
	private static void OnInitComponentHandler
		( EcsactEvent  ev
		, Int32        entityId
		, Int32        componentId
		, IntPtr       componentData
		, IntPtr       callbackUserData
		)
	{
		UnityEngine.Debug.Assert(ev == EcsactEvent.InitComponent);

		var self = (GCHandle.FromIntPtr(callbackUserData).Target as EcsactRuntime)!;
		var componentObject = Ecsact.Util.PtrToComponent(
			componentData,
			componentId
		);
		componentObject = Ecsact.Util.HandlePtrToComponent(ref componentObject);
		self._TriggerInitComponentEvent(entityId, componentId, componentObject);
	}

	[AOT.MonoPInvokeCallback(typeof(ComponentEventCallback))]
	private static void OnUpdateComponentHandler
		( EcsactEvent  ev
		, Int32        entityId
		, Int32        componentId
		, IntPtr       componentData
		, IntPtr       callbackUserData
		)
	{
		UnityEngine.Debug.Assert(ev == EcsactEvent.UpdateComponent);

		var self = (GCHandle.FromIntPtr(callbackUserData).Target as EcsactRuntime)!;
		var componentObject = Ecsact.Util.PtrToComponent(
			componentData,
			componentId
		);
		componentObject = Ecsact.Util.HandlePtrToComponent(ref componentObject);
		self._TriggerUpdateComponentEvent(entityId, componentId, componentObject);
	}

	[AOT.MonoPInvokeCallback(typeof(ComponentEventCallback))]
	private static void OnRemoveComponentHandler
		( EcsactEvent  ev
		, Int32        entityId
		, Int32        componentId
		, IntPtr       componentData
		, IntPtr       callbackUserData
		)
	{
		UnityEngine.Debug.Assert(ev == EcsactEvent.RemoveComponent);

		var self = (GCHandle.FromIntPtr(callbackUserData).Target as EcsactRuntime)!;
		var componentObject = Ecsact.Util.PtrToComponent(
			componentData,
			componentId
		);
		componentObject = Ecsact.Util.HandlePtrToComponent(ref componentObject);
		self._TriggerRemoveComponentEvent(entityId, componentId, componentObject);
	}

}
