#ifndef ECSACT_RUNTIME_META_H
#define ECSACT_RUNTIME_META_H

#include <stdlib.h>

#include <ecsact/runtime/common.h>

#ifndef ECSACT_META_API_VISIBILITY
#	ifdef ECSACT_META_API_LOAD_AT_RUNTIME
#		define ECSACT_META_API_VISIBILITY
#	else
#		ifdef ECSACT_META_API_EXPORT
#			ifdef _WIN32
#				define ECSACT_META_API_VISIBILITY __declspec(dllexport)
#			else
#				define ECSACT_META_API_VISIBILITY __attribute__((visibility("default")))
#			endif
#		else
#			ifdef _WIN32
#				define ECSACT_META_API_VISIBILITY __declspec(dllimport)
#			else
#				define ECSACT_META_API_VISIBILITY
#			endif
#		endif
#	endif
#endif // ECSACT_META_API_VISIBILITY

#ifndef ECSACT_META_API
#	ifdef __cplusplus
#		define ECSACT_META_API extern "C" ECSACT_META_API_VISIBILITY
#	else
#		define ECSACT_META_API extern ECSACT_META_API_VISIBILITY
# endif
#endif // ECSACT_META_API

#ifndef ECSACT_META_API_FN
#	ifdef ECSACT_META_API_LOAD_AT_RUNTIME
#		define ECSACT_META_API_FN(ret, name) ECSACT_META_API ret (*name)
#	else
#		define ECSACT_META_API_FN(ret, name) ECSACT_META_API ret name
#	endif
#endif // ECSACT_META_API_FN

/**
 * Get a registry name. May or may not return originally set name when creating
 * the registry. Typically not returned in release/optimized build.
 */
ECSACT_META_API_FN(const char*, ecsact_meta_registry_name)
	( ecsact_registry_id
	);

/**
 * Get the component data size
 */
ECSACT_META_API_FN(size_t, ecsact_meta_component_size)
	( ecsact_component_id
	);

/**
 * Get the component name. May or may not return originally set name when
 * creating a component. Typically not returned in release/optimized build.
 */
ECSACT_META_API_FN(const char*, ecsact_meta_component_name)
	( ecsact_component_id
	);

/**
 * Get the action data size
 */
ECSACT_META_API_FN(size_t, ecsact_meta_action_size)
	( ecsact_system_id
	);

/**
 * Get the action name. May or may not return originally set name when
 * creating a action. Typically not returned in release/optimized build.
 */
ECSACT_META_API_FN(const char*, ecsact_meta_action_name)
	( ecsact_system_id
	);

/**
 * Get the system name. May or may not return originally set name when
 * creating a system. Typically not returned in release/optimized build.
 */
ECSACT_META_API_FN(const char*, ecsact_meta_system_name)
	( ecsact_system_id
	);

/**
 * Get the count of capabilities for a particular system/action.
 */
ECSACT_META_API_FN(size_t, ecsact_meta_system_capabilities_count)
	( ecsact_system_id system_id
	);

/**
 * Get all the system/action capabilities
 * @param out_capability_component_ids - Output for component ids. Should be 
 * allocated to the size returned from `ecsact_meta_system_capabilities_count`
 * @param out_capabilities - Output for capabilities. Should be allocated to the
 * size returned from `ecsact_meta_system_capabilities_count`
 */
ECSACT_META_API_FN(void, ecsact_meta_system_capabilities)
	( ecsact_system_id           system_id
	, ecsact_component_id*       out_capability_component_ids
	, ecsact_system_capability*  out_capabilities
	);

#define FOR_EACH_ECSACT_META_API_FN(fn, ...)\
	fn(ecsact_meta_registry_name, __VA_ARGS__);\
	fn(ecsact_meta_component_size, __VA_ARGS__);\
	fn(ecsact_meta_component_name, __VA_ARGS__);\
	fn(ecsact_meta_action_size, __VA_ARGS__);\
	fn(ecsact_meta_action_name, __VA_ARGS__);\
	fn(ecsact_meta_system_name, __VA_ARGS__);\
	fn(ecsact_meta_system_capabilities_count, __VA_ARGS__);\
	fn(ecsact_meta_system_capabilities, __VA_ARGS__)

#endif // ECSACT_RUNTIME_META_H
