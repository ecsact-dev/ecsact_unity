#ifndef ECSACT_RUNTIME_DYNAMIC_H
#define ECSACT_RUNTIME_DYNAMIC_H

#include <stdlib.h>

#include <ecsact/runtime/common.h>

#ifndef ECSACT_DYNAMIC_API
#	ifdef _WIN32
#		ifdef ECSACT_DYNAMIC_API_EXPORT
#			ifdef __cplusplus
#				define ECSACT_DYNAMIC_API extern "C" __declspec(dllexport)
#			else
#				define ECSACT_DYNAMIC_API extern __declspec(dllexport)
#			endif
#		else
#			ifdef __cplusplus
#				define ECSACT_DYNAMIC_API extern "C" __declspec(dllimport)
#			else
#				define ECSACT_DYNAMIC_API extern __declspec(dllimport)
#			endif
#		endif
#	else 
#		ifdef ECSACT_DYNAMIC_API_EXPORT
#			ifdef __cplusplus
#				define ECSACT_DYNAMIC_API\
					extern "C" __attribute__((visibility("default")))
#			else
#				define ECSACT_DYNAMIC_API extern __attribute__((visibility("default")))
#			endif
#		else
#			ifdef __cplusplus
#				define ECSACT_DYNAMIC_API extern "C"
#			else
#				define ECSACT_DYNAMIC_API extern
#			endif
#		endif
#	endif
#endif // ECSACT_DYNAMIC_API

#ifndef ECSACT_DYNAMIC_API_FN
#	ifdef ECSACT_DYNAMIC_API_LOAD_AT_RUNTIME
#		define ECSACT_DYNAMIC_API_FN(return_type, fn_name)\
			ECSACT_DYNAMIC_API return_type (*fn_name)
#	else
#		define ECSACT_DYNAMIC_API_FN(return_type, fn_name)\
			ECSACT_DYNAMIC_API return_type fn_name
#	endif
#endif // ECSACT_DYNAMIC_API_FN

/**
 * Get the action data. The caller must allocate the memory for the action data.
 * 
 * NOTE: It is considered an error to call this method on a non-action execution
 * context.
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_action)
	( ecsact_system_execution_context*  context
	, void*                             out_action_data
	);

/**
 * Add new component to the entity currently being processed by the system.
 * 
 * Only available if has one of these capabilities: 
 *  - `ECSACT_SYS_CAP_ADDS`
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_add)
	( ecsact_system_execution_context*  context
	, ecsact_component_id               component_id
	, const void*                       component_data
	);

/**
 * Remove existing component from the entity currently being processed by the
 * system.
 * 
 * Only available if has one of these capabilities:
 *  - `ECSACT_SYS_CAP_REMOVES`
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_remove)
	( ecsact_system_execution_context*  context
	, ecsact_component_id               component_id
	);

/**
 * Get data for component with ID `component_id`. caller must allocate the
 * memory required for the component data.
 *
 * NOTE: It is considered an error if `get` is called without first checking 
 * `has` when the system only has 'optional' capabilities.
 *
 * Only available if has one of these capabilities:
 *  - `ECSACT_SYS_CAP_READONLY`
 *  - `ECSACT_SYS_CAP_READWRITE`
 *  - `ECSACT_SYS_CAP_OPTIONAL_READONLY`
 *  - `ECSACT_SYS_CAP_OPTIONAL_READWRITE`
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_get)
	( ecsact_system_execution_context*  context
	, ecsact_component_id               component_id
	, void*                             out_component_data
	);

/**
 * Only available if has one of these capabilities:
 *  - `ECSACT_SYS_CAP_WRITEONLY`
 *  - `ECSACT_SYS_CAP_READWRITE`
 *  - `ECSACT_SYS_CAP_OPTIONAL_WRITEONLY`
 *  - `ECSACT_SYS_CAP_OPTIONAL_READWRITE`
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_update)
	( ecsact_system_execution_context*  context
	, ecsact_component_id               component_id
	, const void*                       component_data
	);

/**
 * Check if the component with ID `component_id` exists on the entity 
 * currently being processed  by the system.
 * 
 * Only available if has one of these capabilities:
 *  - `ECSACT_SYS_CAP_OPTIONAL_READONLY` 
 *  - `ECSACT_SYS_CAP_OPTIONAL_WRITEONLY`
 *  - `ECSACT_SYS_CAP_OPTIONAL_READWRITE`
 */
ECSACT_DYNAMIC_API_FN(bool, ecsact_system_execution_context_has)
	( ecsact_system_execution_context*  context
	, ecsact_component_id               component_id
	);

/**
 * Generate a new entity with specified components.
 * 
 * @param component_count length of `component_ids` and `components_data`
 * @param component_ids list of component ids associatd with `components_data`.
 * @param components_data list of component data associated with 
 *        `component_ids`.
 * 
 * @note Only available if the system is a generator. @see 
 * `ecsact_add_system_generate_component_set`
 */
ECSACT_DYNAMIC_API_FN(void, ecsact_system_execution_context_generate)
	( ecsact_system_execution_context*  context
	, int                               component_count
	, ecsact_component_id*              component_ids
	, const void**                      components_data
	);

/**
 * Get the parent system exeuction context.
 * 
 * Only available if the currently executing system is a nested system.
 */
ECSACT_DYNAMIC_API_FN( const ecsact_system_execution_context*
                     , ecsact_system_execution_context_parent )
	( ecsact_system_execution_context*  context
	);

/**
 * Check if two execution contexts refer to the same entity. This is useful when
 * comparing against parent execution context to skip
 * 
 * NOTE: This is will eventually be deprecated in favour of a language feature
 *       to skip matching parent system entities.
 */
ECSACT_DYNAMIC_API_FN(bool, ecsact_system_execution_context_same)
	( const ecsact_system_execution_context*
	, const ecsact_system_execution_context*
	);

/**
 * Get the current system/action ID
 */
ECSACT_DYNAMIC_API_FN(ecsact_system_id, ecsact_system_execution_context_id)
	( ecsact_system_execution_context* context
	);

/**
 * Create a new system declaration at runtime.
 * @param system_name (Optional) Display name for the system. Only used for 
 * debugging.
 * @param parent_sytsem_id (Optional) When creating a nested system there must 
 * be a parent system ID. Pass `ecsact_invalid_system_id` if no parent is to 
 * be set. May be an action or a regular system ID.
 * @param capability_component_ids (Optional) List of component ids that match
 * `capabilities` and holds at least `capabilities_count`
 * @param capabilities (Optional) List of capabilities that match
 * `capability_component_ids` and holds at least `capabilities_count`
 * @param capabilities_count (Optional) Element count inside `capabilities` and
 * `capability_component_ids`
 * @return The newly created system ID. NOTE: may return 
 * `ecsact_invalid_system_id` if the current runtime does not support dynamic 
 * systems.
 */
ECSACT_DYNAMIC_API_FN(ecsact_system_id, ecsact_create_system)
	( const char*                      system_name
	, ecsact_system_id                 parent_system_id
	, const ecsact_component_id*       capability_component_ids
	, const ecsact_system_capability*  capabilities
	, size_t                           capabilities_count
	, ecsact_system_execution_impl     execution_impl
	);

/**
 * Systems execute in the order they are registered in. If the order needs to be
 * adjusted this method can be used to move systems before or after other
 * systems.
 */
ECSACT_DYNAMIC_API_FN(bool, ecsact_reorder_system)
	( ecsact_system_id  target_system_id
	, ecsact_system_id  relative_system_id
	, bool              target_before_relative
	);

/**
 * Sets the system execution implementation function. If one is already set it
 * gets overwritten.
 * 
 * NOTE: ONLY `ecsact_system_execution_context_*` functions are allowed to be
 *       called while a system is executing.
 */
ECSACT_DYNAMIC_API_FN(bool, ecsact_set_system_execution_impl)
	( ecsact_system_id              system_id
	, ecsact_system_execution_impl  system_exec_impl
	);

/**
 * Create a new action declaration at runtime.
 * @param action_name (Optional) Display name for the action. Only used for 
 * debugging.
 * @param capability_component_ids (Optional) List of component ids that match
 * `capabilities` and holds at least `capabilities_count`
 * @param capabilities (Optional) List of capabilities that match
 * `capability_component_ids` and holds at least `capabilities_count`
 * @param capabilities_count (Optional) Element count inside `capabilities` and
 * `capability_component_ids`
 * @return The newly created system ID. NOTE: may return 
 * `ecsact_invalid_system_id` if the current runtime does not support dynamic 
 * actions.
 */
ECSACT_DYNAMIC_API_FN(ecsact_system_id, ecsact_create_action)
	( const char*                      action_name
	, size_t                           action_size
	, ecsact_action_compare_fn_t       action_compare_fn
	, const ecsact_component_id*       capability_component_ids
	, const ecsact_system_capability*  capabilities
	, size_t                           capabilities_count
	, ecsact_system_execution_impl     execution_impl
	);

ECSACT_DYNAMIC_API_FN(ecsact_system_id, ecsact_resize_action)
	( ecsact_system_id            action_id
	, size_t                      new_action_size
	, ecsact_action_compare_fn_t  new_action_compare_fn
	);

/**
 * Create a new component declaration at runtime.
 * @param component_name (Optional) Display name for the component. Only used
 * for debugging.
 * @param component_size Allocation size of component in bytes. Typically this
 * value is `sizeof(struct my_struct)`
 * @return The newly created component ID. NOTE: may return
 * `ecsact_invalid_component_id` if the current runtime does not support
 * dynamic components.
 */
ECSACT_DYNAMIC_API_FN(ecsact_component_id, ecsact_create_component)
	( const char*                    component_name
	, size_t                         component_size
	, ecsact_component_compare_fn_t  component_compare_fn
	);

ECSACT_DYNAMIC_API_FN(void, ecsact_resize_component)
	( ecsact_component_id            component_id
	, size_t                         new_component_size
	, ecsact_component_compare_fn_t  new_component_compare_fn
	);

ECSACT_DYNAMIC_API_FN(void, ecsact_destroy_component)
	( ecsact_component_id component_id
	);

ECSACT_DYNAMIC_API_FN(ecsact_variant_id, ecsact_create_variant)
	( const char*                     variant_name
	, size_t                          alternatives_count
	, ecsact_component_id*            out_alternative_ids
	, const char**                    alternative_names
	, size_t*                         alternative_sizes
	, ecsact_component_compare_fn_t*  alternative_compare_fns
	);

ECSACT_DYNAMIC_API_FN(void, ecsact_destroy_variant)
	( ecsact_variant_id variant_id
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_add_system_capability)
	( ecsact_system_id
	, ecsact_component_id
	, ecsact_system_capability
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_update_system_capability)
	( ecsact_system_id
	, ecsact_component_id
	, ecsact_system_capability
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_remove_system_capability)
	( ecsact_system_id
	, ecsact_component_id
	);

/**
 * Adds a set of component ids that this system may use to generate new
 * entities.
 * 
 * @note there is no way to remove a system generate component set. If it is
 *       a requirement, however, you may destroy the system and re-create it
 *       without the generate component set(s).
 * 
 * @return returns -1 if unsuccessful otherwise returns unspecified value
 */
ECSACT_DYNAMIC_API_FN(int, ecsact_add_system_generate_component_set)
	( ecsact_system_id         system_id
	, int                      components_count
	, ecsact_component_id*     component_ids
	, ecsact_system_generate*  component_generate_flags
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_register_component)
	( ecsact_registry_id
	, ecsact_component_id
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_register_system)
	( ecsact_registry_id
	, ecsact_system_id
	);

ECSACT_DYNAMIC_API_FN(bool, ecsact_register_action)
	( ecsact_registry_id
	, ecsact_system_id
	);

#define FOR_EACH_ECSACT_DYNAMIC_API_FN(fn, ...)\
	fn(ecsact_system_execution_context_action, __VA_ARGS__);\
	fn(ecsact_system_execution_context_add, __VA_ARGS__);\
	fn(ecsact_system_execution_context_remove, __VA_ARGS__);\
	fn(ecsact_system_execution_context_get, __VA_ARGS__);\
	fn(ecsact_system_execution_context_has, __VA_ARGS__);\
	fn(ecsact_system_execution_context_generate, __VA_ARGS__);\
	fn(ecsact_system_execution_context_parent, __VA_ARGS__);\
	fn(ecsact_system_execution_context_same, __VA_ARGS__);\
	fn(ecsact_create_system, __VA_ARGS__);\
	fn(ecsact_set_system_execution_impl, __VA_ARGS__);\
	fn(ecsact_create_action, __VA_ARGS__);\
	fn(ecsact_resize_action, __VA_ARGS__);\
	fn(ecsact_create_component, __VA_ARGS__);\
	fn(ecsact_resize_component, __VA_ARGS__);\
	fn(ecsact_destroy_component, __VA_ARGS__);\
	fn(ecsact_create_variant, __VA_ARGS__);\
	fn(ecsact_destroy_variant, __VA_ARGS__);\
	fn(ecsact_add_system_capability, __VA_ARGS__);\
	fn(ecsact_update_system_capability, __VA_ARGS__);\
	fn(ecsact_remove_system_capability, __VA_ARGS__);\
	fn(ecsact_add_system_generate_component_set, __VA_ARGS__);\
	fn(ecsact_register_component, __VA_ARGS__);\
	fn(ecsact_register_system, __VA_ARGS__);\
	fn(ecsact_register_action, __VA_ARGS__);\
	fn(ecsact_system_execution_context_id, __VA_ARGS__)

#undef ECSACT_DYNAMIC_API
#undef ECSACT_DYNAMIC_API_FN
#endif // ECSACT_RUNTIME_DYNAMIC_H
