#ifndef ECSACT_RUNTIME_CORE_H
#define ECSACT_RUNTIME_CORE_H

#include <stdint.h>
#include <stdbool.h>
#include <ecsact/runtime/common.h>

#ifndef ECSACT_CORE_API_VISIBILITY
#	ifdef ECSACT_CORE_API_LOAD_AT_RUNTIME
#		define ECSACT_CORE_API_VISIBILITY
#	else
#		ifdef ECSACT_CORE_API_EXPORT
#			ifdef _WIN32
#				define ECSACT_CORE_API_VISIBILITY __declspec(dllexport)
#			else
#				define ECSACT_CORE_API_VISIBILITY __attribute__((visibility("default")))
#			endif
#		else
#			ifdef _WIN32
#				define ECSACT_CORE_API_VISIBILITY __declspec(dllimport)
#			else
#				define ECSACT_CORE_API_VISIBILITY
#			endif
#		endif
#	endif
#endif // ECSACT_CORE_API_VISIBILITY

#ifndef ECSACT_CORE_API
#	ifdef __cplusplus
#		define ECSACT_CORE_API extern "C" ECSACT_CORE_API_VISIBILITY
#	else
#		define ECSACT_CORE_API extern ECSACT_CORE_API_VISIBILITY
# endif
#endif // ECSACT_CORE_API

#ifndef ECSACT_CORE_API_FN
#	ifdef ECSACT_CORE_API_LOAD_AT_RUNTIME
#		define ECSACT_CORE_API_FN(ret, name) ECSACT_CORE_API ret (*name)
#	else
#		define ECSACT_CORE_API_FN(ret, name) ECSACT_CORE_API ret name
#	endif
#endif // ECSACT_CORE_API_FN

/**
 * Create a new registry.
 * @param registry_name (Optional) Display name for the registry. Only used for
 * debugging.
 * @return The newly created registry ID.
 */
ECSACT_CORE_API_FN(ecsact_registry_id, ecsact_create_registry)
	( const char* registry_name
	);

ECSACT_CORE_API_FN(void, ecsact_destroy_registry)
	( ecsact_registry_id
	);

/**
 * Destroy all entities
 */
ECSACT_CORE_API_FN(void, ecsact_clear_registry)
	( ecsact_registry_id
	);

/**
 * Create an entity and return the ID
 */
ECSACT_CORE_API_FN(ecsact_entity_id, ecsact_create_entity)
	( ecsact_registry_id
	);

/**
 * Ensure an entity with the provided ID exists on the registry. If the entity
 * does not exist it will be created.
 * 
 * NOTE: Avoid this method if possible.
 */
ECSACT_CORE_API_FN(void, ecsact_ensure_entity)
	( ecsact_registry_id
	, ecsact_entity_id
	);

/**
 * Check if entity exists.
 * 
 * NOTE: Avoid this method if possible.
 */
ECSACT_CORE_API_FN(bool, ecsact_entity_exists)
	( ecsact_registry_id
	, ecsact_entity_id
	);

ECSACT_CORE_API_FN(void, ecsact_destroy_entity)
	( ecsact_registry_id
	, ecsact_entity_id
	);

/**
 * Count number of entites in registry
 */
ECSACT_CORE_API_FN(int, ecsact_count_entities)
	( ecsact_registry_id  registry
	);

/**
 * Get list of entities in registry
 */
ECSACT_CORE_API_FN(void, ecsact_get_entities)
	( ecsact_registry_id  registry
	, int                 max_entities_count
	, ecsact_entity_id*   out_entities
	, int*                out_entities_count
	);

ECSACT_CORE_API_FN(void, ecsact_add_component)
	( ecsact_registry_id
	, ecsact_entity_id
	, ecsact_component_id
	, const void* component_data
	);

ECSACT_CORE_API_FN(bool, ecsact_has_component)
	( ecsact_registry_id
	, ecsact_entity_id
	, ecsact_component_id
	);

ECSACT_CORE_API_FN(const void*, ecsact_get_component)
	( ecsact_registry_id
	, ecsact_entity_id
	, ecsact_component_id
	);

ECSACT_CORE_API_FN(int, ecsact_count_components)
	( ecsact_registry_id  registry_id
	, ecsact_entity_id    entity_id
	);

ECSACT_CORE_API_FN(void, ecsact_get_components)
	( ecsact_registry_id     registry_id
	, ecsact_entity_id       entity_id
	, int                    max_components_count
	, ecsact_component_id*   out_component_ids
	, const void**           out_components_data
	, int*                   out_components_count
	);

typedef void(*ecsact_each_component_callback)
	( ecsact_component_id  component_id
	, const void*          component_data
	, void*                user_data
	);

/**
 * Invoke `callback` for every component an entity has
 */
ECSACT_CORE_API_FN(void, ecsact_each_component)
	( ecsact_registry_id              registry_id
	, ecsact_entity_id                entity_id
	, ecsact_each_component_callback  callback
	, void*                           callback_user_data
	);

ECSACT_CORE_API_FN(void, ecsact_update_component)
	( ecsact_registry_id
	, ecsact_entity_id
	, ecsact_component_id
	, const void* component_data
	);

ECSACT_CORE_API_FN(void, ecsact_remove_component)
	( ecsact_registry_id
	, ecsact_entity_id
	, ecsact_component_id
	);

/**
 * Struct representing a single action
 */
typedef struct ecsact_action {
	/**
	 * ID of action originally given by `ecsact_register_action` or the statically
	 * known ID in the case of a compile time action.
	 */
	ecsact_system_id action_id;

	/**
	 * Pointer to action data. Size is determined by the registerd action
	 * associated with the `action_id`.
	 */
	const void* action_data;
} ecsact_action;

/**
 * Struct representing a single component
 */
typedef struct ecsact_component {
	/**
	 * ID of component originally given by `ecsact_register_component` or the 
	 * statically known ID in the case of a compile time component.
	 */
	ecsact_component_id component_id;

	/**
	 * Pointer to component data. Size is determined by the registerd component
	 * associated with the `component_id`.
	 */
	const void* component_data;
} ecsact_component;

/**
 * Options related to system execution to be passed to `ecsact_execute_systems`
 */
typedef struct ecsact_execution_options {
	/**
	 * Length of `add_components_entities` and `add_components` sequential lists.
	 */
	int add_components_length;

	/**
	 * Sequential list of entities that will have components added determined by 
	 * `add_components` before any system or action execution occurs. Length is
	 * determined by `add_components_length`.
	 */
	ecsact_entity_id* add_components_entities;

	/**
	 * Sequential list of components that will be added to the entities listed in
	 * `add_components_entities`. Length is determined by `add_components_length`.
	 */
	ecsact_component* add_components;

	/**
	 * Length of `update_components_entities` and `update_components` sequential 
	 * lists.
	 */
	int update_components_length;

	/**
	 * Sequential list of entities that will have components updated determined 
	 * by `update_components` before any system or action execution occurs. Length is determined by `update_components_length`.
	 */
	ecsact_entity_id* update_components_entities;

	/**
	 * Sequential list of components that will be updated on the entities listed 
	 * in `update_components_entities`. Length is determined by 
	 * `update_components_length`.
	 */
	ecsact_component* update_components;

	/**
	 * Length of `remove_components_entities` and `remove_components` sequential 
	 * lists.
	 */
	int remove_components_length;

	/**
	 * Sequential list of entities that will have components removed determined 
	 * by `remove_components` before any system or action execution occurs. 
	 * Length is determined by `remove_components_length`.
	 */
	ecsact_entity_id* remove_components_entities;

	/**
	 * Sequential list of component IDs that will be removed on the entities 
	 * listed in `remove_components_entities`. Length is determined by 
	 * `remove_components_length`.
	 */
	ecsact_component_id* remove_components;

	/**
	 * Length of `actions` sequential list.
	 */
	int actions_length;

	/**
	 * Sequential list of actions to be executed.
	 */
	ecsact_action* actions;
} ecsact_execution_options;

typedef enum {
	/**
	 * Initialized component - Newly added component during execution.
	 * 
	 * `component_data` does not necessarily reflect the component_data when the 
	 * component was original added due to the component potentially being
	 * modified during system execution or (in the case of multi-execution calls)
	 * modification across executions.
	 */
	ECSACT_EVENT_INIT_COMPONENT = 0,

	/**
	 * Update component - Component has been modified during execution.
	 * 
	 * This event never occurs for components without any fields (tag components).
	 * 
	 * If a component is modified during execution 
	 */
	ECSACT_EVENT_UPDATE_COMPONENT = 1,

	/**
	 * Remove component - Component has been removed during execution.
	 * 
	 * If a component is removed and then re-added during single or 
	 * multi-execution calls this event will not occur. The event only occurs 
	 * when at the end of the execution call the component is removed.
	 */
	ECSACT_EVENT_REMOVE_COMPONENT = 2,
} ecsact_event;

/**
 * Event handler callback
 */
typedef void (*ecsact_component_event_callback)
	( ecsact_event         event
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	, const void*          component_data
	, void*                callback_user_data
	);

/**
 * Holds event handler callbacks and their user data
 */
typedef struct ecsact_execution_events_collector {
	/**
	 * invoked after system executions are finished for every component that is 
	 * new. The component_data is the last value given for the component, not the 
	 * first. Invocation happens in the calling thread. `event` will always be 
	 * `ECSACT_EVENT_INIT_COMPONENT`
	 */
	ecsact_component_event_callback init_callback;

	/**
	 * `callback_user_data` passed to `init_callback`
	 */
	void* init_callback_user_data;

	/**
	 * invoked after system executions are finished for every changed component. 
	 * Invocation happens in the calling thread. `event` will always be 
	 * `ECSACT_EVENT_UPDATE_COMPONENT`
	 */
	ecsact_component_event_callback update_callback;

	/**
	 * `callback_user_data` passed to `update_callback`
	 */
	void* update_callback_user_data;

	/**
	 * invoked after system executions are finished for every removed component. 
	 * Invocation happens in the calling thread. `event` will will always be 
	 * `ECSACT_EVENT_REMOVE_COMPONENT`.
	 */
	ecsact_component_event_callback remove_callback;

	/**
	 * `callback_user_data` passed to `remove_callback`
	 */
	void* remove_callback_user_data;
} ecsact_execution_events_collector;

/**
 * Execute system implementations for all registered systems and pushed actions 
 * against all registered components. System implementations may run in parallel
 * on multiple threads.
 * @param execution_count how many times the systems list should execute
 * @param execution_options_list (optional) Seqential list of execution options.
 *        If set (not NULL), list length is determined by `execution_count`.
 * @param events_collector (optional) Pointer to events collector. If set,
 *        events will be recorded and the callbacks on the collector will be
 *        invoked. Invocations occur on the calling thread.
 */
ECSACT_CORE_API_FN(void, ecsact_execute_systems)
	( ecsact_registry_id                        registry_id
	, int                                       execution_count
	, const ecsact_execution_options*           execution_options_list
	, const ecsact_execution_events_collector*  events_collector
	);

#define FOR_EACH_ECSACT_CORE_API_FN(fn, ...)\
	fn(ecsact_create_registry, __VA_ARGS__);\
	fn(ecsact_destroy_registry, __VA_ARGS__);\
	fn(ecsact_clear_registry, __VA_ARGS__);\
	fn(ecsact_create_entity, __VA_ARGS__);\
	fn(ecsact_ensure_entity, __VA_ARGS__);\
	fn(ecsact_entity_exists, __VA_ARGS__);\
	fn(ecsact_destroy_entity, __VA_ARGS__);\
	fn(ecsact_count_entities, __VA_ARGS__);\
	fn(ecsact_get_entities, __VA_ARGS__);\
	fn(ecsact_add_component, __VA_ARGS__);\
	fn(ecsact_has_component, __VA_ARGS__);\
	fn(ecsact_get_component, __VA_ARGS__);\
	fn(ecsact_each_component, __VA_ARGS__);\
	fn(ecsact_count_components, __VA_ARGS__);\
	fn(ecsact_get_components, __VA_ARGS__);\
	fn(ecsact_update_component, __VA_ARGS__);\
	fn(ecsact_remove_component, __VA_ARGS__);\
	fn(ecsact_execute_systems, __VA_ARGS__)

#undef ECSACT_CORE_API
#undef ECSACT_CORE_API_FN
#endif // ECSACT_RUNTIME_CORE_H
