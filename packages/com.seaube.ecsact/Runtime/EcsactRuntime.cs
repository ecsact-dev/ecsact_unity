using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
internal static class NativeLibrary {
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
		return LoadLibrary(libraryPath);
	}

	public static void Free
		( IntPtr handle
		)
	{
		FreeLibrary(handle);
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
	public enum EcsactEvent : Int32 {
		InitComponent = 0,
		UpdateComponent = 1,
		RemoveComponent = 2,
	}

	public delegate void EachComponentCallback
		( Int32   componentId
		, IntPtr  componentData
		, IntPtr  callbackUserData
		);

	public delegate void ComponentEventCallback
		( EcsactEvent  ev
		, Int32        entityId
		, Int32        componentId
		, object       componentData
		, IntPtr       callbackUserData
		);

	public struct ExecutionOptions {

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

	public static string[] dynamicMethods => new string[]{
		"ecsact_system_execution_context_action",
		"ecsact_system_execution_context_add",
		"ecsact_system_execution_context_remove",
		"ecsact_system_execution_context_get",
		"ecsact_system_execution_context_has",
		"ecsact_system_execution_context_generate",
		"ecsact_system_execution_context_parent",
		"ecsact_system_execution_context_same",
		"ecsact_create_system",
		"ecsact_set_system_execution_impl",
		"ecsact_create_action",
		"ecsact_resize_action",
		"ecsact_create_component",
		"ecsact_resize_component",
		"ecsact_destroy_component",
		"ecsact_create_variant",
		"ecsact_destroy_variant",
		"ecsact_add_system_capability",
		"ecsact_update_system_capability",
		"ecsact_remove_system_capability",
		"ecsact_add_system_generate_component_set",
		"ecsact_register_component",
		"ecsact_register_system",
		"ecsact_register_action",
		"ecsact_system_execution_context_id",
	};

	public static string[] metaMethods => new string[]{
		"ecsact_meta_registry_name",
		"ecsact_meta_component_size",
		"ecsact_meta_component_name",
		"ecsact_meta_action_size",
		"ecsact_meta_action_name",
		"ecsact_meta_system_name",
		"ecsact_meta_system_capabilities_count",
		"ecsact_meta_system_capabilities",
	};

	public static string[] serializeMethods => new string[]{
		"ecsact_serialize_action_size",
		"ecsact_serialize_component_size",
		"ecsact_serialize_action",
		"ecsact_serialize_component",
		"ecsact_deserialize_action",
		"ecsact_deserialize_component",
	};

	public static string[] staticMethods => new string[]{
		"ecsact_static_components",
		"ecsact_static_variants",
		"ecsact_static_systems",
		"ecsact_static_actions",
		"ecsact_static_on_reload",
		"ecsact_static_off_reload",
	};

	private static EcsactRuntime? defaultInstance;

	public static EcsactRuntime GetOrLoadDefault() {
		if(defaultInstance == null) {
			var settings = EcsactRuntimeSettings.Get();
			defaultInstance = Load(settings.runtimeLibraryPaths);
		}

		return defaultInstance;
	}

	public static void SetDefault
		( EcsactRuntime? runtime
		)
	{
		if(defaultInstance != null) {
			Free(defaultInstance);
		}
		defaultInstance = runtime;
	}

	private IntPtr[]? _libs;
	private Core? _core;

	public class Core {
		internal List<string> _availableMethods = new();
		public static string[] methods => new string[]{
			"ecsact_create_registry",
			"ecsact_destroy_registry",
			"ecsact_clear_registry",
			"ecsact_create_entity",
			"ecsact_ensure_entity",
			"ecsact_entity_exists",
			"ecsact_destroy_entity",
			"ecsact_count_entities",
			"ecsact_get_entities",
			"ecsact_add_component",
			"ecsact_has_component",
			"ecsact_get_component",
			"ecsact_each_component",
			"ecsact_count_components",
			"ecsact_get_components",
			"ecsact_update_component",
			"ecsact_remove_component",
			"ecsact_execute_systems",
		};

		public IEnumerable<string> availableMethods => _availableMethods;

		// Core module methods
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

		internal delegate void ecsact_add_component_delegate
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
			, out Int32[]   outComponentIds
			, out IntPtr[]  outComponentsData
			, out Int32     outComponentsCount
			);
		internal ecsact_get_components_delegate? ecsact_get_components;

		internal delegate void ecsact_update_component_delegate
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
			, IntPtr       componentData
			, IntPtr       callbackUserData
			);
		internal delegate void ecsact_execute_systems_delegate
			( Int32                     registryId
			, Int32                     executionCount
			, ExecutionOptions[]        executionOptionsList
			, ExecutionEventsCollector  eventsCollector
			);
		internal ecsact_execute_systems_delegate? ecsact_execute_systems;

		public Int32 CreateRegistry
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

		public void AddComponent
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			, IntPtr  componentData
			)
		{
			if(ecsact_add_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_add_component");
			}

			ecsact_add_component(
				registryId,
				entityId,
				componentId,
				componentData
			);
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

		public IntPtr GetComponent
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			)
		{
			if(ecsact_get_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_component");
			}

			return ecsact_get_component(registryId, entityId, componentId);
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

		public void GetComponents
			( Int32         registryId
			, Int32         entityId
			, Int32         maxComponentsCount
			, out Int32[]   outComponentIds
			, out IntPtr[]  outComponentsData
			, out Int32     outComponentsCount
			)
		{
			if(ecsact_get_components == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_get_components");
			}

			ecsact_get_components(
				registryId,
				entityId,
				maxComponentsCount,
				out outComponentIds,
				out outComponentsData,
				out outComponentsCount
			);
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

		public void UpdateComponent
			( Int32   registryId
			, Int32   entityId
			, Int32   componentId
			, IntPtr  componentData
			)
		{
			if(ecsact_update_component == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_update_component");
			}

			ecsact_update_component(
				registryId,
				entityId,
				componentId,
				componentData
			);
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

			ecsact_remove_component(registryId, entityId, componentId);
		}

		public void ExecuteSystems
			( Int32                     registryId
			, Int32                     executionCount
			, ExecutionOptions[]        executionOptionsList
			, ExecutionEventsCollector  eventsCollector
			)
		{
			if(ecsact_execute_systems == null) {
				throw new EcsactRuntimeMissingMethod("ecsact_execute_systems");
			}

			ecsact_execute_systems(
				registryId,
				executionCount,
				executionOptionsList,
				eventsCollector
			);
		}
	}

	public Core core => _core!;

	private static void LoadDelegate<D>
		( IntPtr        lib
		, string        name
		, out D?        outDelegate
		, List<string>  availableMethods
		) where D : Delegate 
	{
		IntPtr addr;
		if(NativeLibrary.TryGetExport(lib, name, out addr)) {
			outDelegate = Marshal.GetDelegateForFunctionPointer<D>(addr);
			availableMethods.Add(name);
			UnityEngine.Debug.Log($"Load {name} SUCCESS");
		} else {
			outDelegate = null;
			UnityEngine.Debug.Log($"Load {name} FAIL");
		}
	}

	public static EcsactRuntime Load
		( IEnumerable<string> libraryPaths
		)
	{
		var runtime = new EcsactRuntime();
		runtime._core = new Core();
		runtime._libs =
			libraryPaths.Select(path => NativeLibrary.Load(path)).ToArray();

		foreach(var lib in runtime._libs) {
			// Load core methods
			LoadDelegate(lib, "ecsact_create_registry", out runtime._core.ecsact_create_registry, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_destroy_registry", out runtime._core.ecsact_destroy_registry, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_clear_registry", out runtime._core.ecsact_clear_registry, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_create_entity", out runtime._core.ecsact_create_entity, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_ensure_entity", out runtime._core.ecsact_ensure_entity, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_entity_exists", out runtime._core.ecsact_entity_exists, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_destroy_entity", out runtime._core.ecsact_destroy_entity, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_count_entities", out runtime._core.ecsact_count_entities, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_get_entities", out runtime._core.ecsact_get_entities, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_add_component", out runtime._core.ecsact_add_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_has_component", out runtime._core.ecsact_has_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_get_component", out runtime._core.ecsact_get_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_each_component", out runtime._core.ecsact_each_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_count_components", out runtime._core.ecsact_count_components, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_get_components", out runtime._core.ecsact_get_components, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_update_component", out runtime._core.ecsact_update_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_remove_component", out runtime._core.ecsact_remove_component, runtime._core._availableMethods);
			LoadDelegate(lib, "ecsact_execute_systems", out runtime._core.ecsact_execute_systems, runtime._core._availableMethods);

			// Load dynamic methods
			// LoadDelegate(lib, "ecsact_system_execution_context_action", out runtime.ecsact_system_execution_context_action);
			// LoadDelegate(lib, "ecsact_system_execution_context_add", out runtime.ecsact_system_execution_context_add);
			// LoadDelegate(lib, "ecsact_system_execution_context_remove", out runtime.ecsact_system_execution_context_remove);
			// LoadDelegate(lib, "ecsact_system_execution_context_get", out runtime.ecsact_system_execution_context_get);
			// LoadDelegate(lib, "ecsact_system_execution_context_has", out runtime.ecsact_system_execution_context_has);
			// LoadDelegate(lib, "ecsact_system_execution_context_generate", out runtime.ecsact_system_execution_context_generate);
			// LoadDelegate(lib, "ecsact_system_execution_context_parent", out runtime.ecsact_system_execution_context_parent);
			// LoadDelegate(lib, "ecsact_system_execution_context_same", out runtime.ecsact_system_execution_context_same);
			// LoadDelegate(lib, "ecsact_create_system", out runtime.ecsact_create_system);
			// LoadDelegate(lib, "ecsact_set_system_execution_impl", out runtime.ecsact_set_system_execution_impl);
			// LoadDelegate(lib, "ecsact_create_action", out runtime.ecsact_create_action);
			// LoadDelegate(lib, "ecsact_resize_action", out runtime.ecsact_resize_action);
			// LoadDelegate(lib, "ecsact_create_component", out runtime.ecsact_create_component);
			// LoadDelegate(lib, "ecsact_resize_component", out runtime.ecsact_resize_component);
			// LoadDelegate(lib, "ecsact_destroy_component", out runtime.ecsact_destroy_component);
			// LoadDelegate(lib, "ecsact_create_variant", out runtime.ecsact_create_variant);
			// LoadDelegate(lib, "ecsact_destroy_variant", out runtime.ecsact_destroy_variant);
			// LoadDelegate(lib, "ecsact_add_system_capability", out runtime.ecsact_add_system_capability);
			// LoadDelegate(lib, "ecsact_update_system_capability", out runtime.ecsact_update_system_capability);
			// LoadDelegate(lib, "ecsact_remove_system_capability", out runtime.ecsact_remove_system_capability);
			// LoadDelegate(lib, "ecsact_add_system_generate_component_set", out runtime.ecsact_add_system_generate_component_set);
			// LoadDelegate(lib, "ecsact_register_component", out runtime.ecsact_register_component);
			// LoadDelegate(lib, "ecsact_register_system", out runtime.ecsact_register_system);
			// LoadDelegate(lib, "ecsact_register_action", out runtime.ecsact_register_action);
			// LoadDelegate(lib, "ecsact_system_execution_context_id", out runtime.ecsact_system_execution_context_id);

			// Load meta methods
			// LoadDelegate(lib, "ecsact_meta_registry_name", out runtime.ecsact_meta_registry_name);
			// LoadDelegate(lib, "ecsact_meta_component_size", out runtime.ecsact_meta_component_size);
			// LoadDelegate(lib, "ecsact_meta_component_name", out runtime.ecsact_meta_component_name);
			// LoadDelegate(lib, "ecsact_meta_action_size", out runtime.ecsact_meta_action_size);
			// LoadDelegate(lib, "ecsact_meta_action_name", out runtime.ecsact_meta_action_name);
			// LoadDelegate(lib, "ecsact_meta_system_name", out runtime.ecsact_meta_system_name);
			// LoadDelegate(lib, "ecsact_meta_system_capabilities_count", out runtime.ecsact_meta_system_capabilities_count);
			// LoadDelegate(lib, "ecsact_meta_system_capabilities", out runtime.ecsact_meta_system_capabilities);

			// Load serialize methods
			// LoadDelegate(lib, "ecsact_serialize_action_size", out runtime.ecsact_serialize_action_size);
			// LoadDelegate(lib, "ecsact_serialize_component_size", out runtime.ecsact_serialize_component_size);
			// LoadDelegate(lib, "ecsact_serialize_action", out runtime.ecsact_serialize_action);
			// LoadDelegate(lib, "ecsact_serialize_component", out runtime.ecsact_serialize_component);
			// LoadDelegate(lib, "ecsact_deserialize_action", out runtime.ecsact_deserialize_action);
			// LoadDelegate(lib, "ecsact_deserialize_component", out runtime.ecsact_deserialize_component);

			// Load static methods
			// LoadDelegate(lib, "ecsact_static_components", out runtime.ecsact_static_components);
			// LoadDelegate(lib, "ecsact_static_variants", out runtime.ecsact_static_variants);
			// LoadDelegate(lib, "ecsact_static_systems", out runtime.ecsact_static_systems);
			// LoadDelegate(lib, "ecsact_static_actions", out runtime.ecsact_static_actions);
			// LoadDelegate(lib, "ecsact_static_on_reload", out runtime.ecsact_static_on_reload);
			// LoadDelegate(lib, "ecsact_static_off_reload", out runtime.ecsact_static_off_reload);
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

	~EcsactRuntime() {
		Free(this);
	}
}
