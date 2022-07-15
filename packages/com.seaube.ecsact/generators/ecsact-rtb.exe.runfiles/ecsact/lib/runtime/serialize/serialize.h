#ifndef ECSACT_RUNTIME_SERIALIZE_H
#define ECSACT_RUNTIME_SERIALIZE_H

#include <stdint.h>
#include <stdbool.h>
#include <ecsact/runtime/common.h>

#ifndef ECSACT_SERIALIZE_API_VISIBILITY
#	ifdef ECSACT_SERIALIZE_API_LOAD_AT_RUNTIME
#		define ECSACT_SERIALIZE_API_VISIBILITY
#	else
#		ifdef ECSACT_SERIALIZE_API_EXPORT
#			ifdef _WIN32
#				define ECSACT_SERIALIZE_API_VISIBILITY __declspec(dllexport)
#			else
#				define ECSACT_SERIALIZE_API_VISIBILITY __attribute__((visibility("default")))
#			endif
#		else
#			ifdef _WIN32
#				define ECSACT_SERIALIZE_API_VISIBILITY __declspec(dllimport)
#			else
#				define ECSACT_SERIALIZE_API_VISIBILITY
#			endif
#		endif
#	endif
#endif // ECSACT_SERIALIZE_API_VISIBILITY

#ifndef ECSACT_SERIALIZE_API
#	ifdef __cplusplus
#		define ECSACT_SERIALIZE_API extern "C" ECSACT_SERIALIZE_API_VISIBILITY
#	else
#		define ECSACT_SERIALIZE_API extern ECSACT_SERIALIZE_API_VISIBILITY
# endif
#endif // ECSACT_SERIALIZE_API

#ifndef ECSACT_SERIALIZE_API_FN
#	ifdef ECSACT_SERIALIZE_API_LOAD_AT_RUNTIME
#		define ECSACT_SERIALIZE_API_FN(ret, name) ECSACT_SERIALIZE_API ret (*name)
#	else
#		define ECSACT_SERIALIZE_API_FN(ret, name) ECSACT_SERIALIZE_API ret name
#	endif
#endif // ECSACT_SERIALIZE_API_FN

/**
 * Get the amount of bytes an action with id `action_id` requies to serialize.
 */
ECSACT_SERIALIZE_API_FN(int, ecsact_serialize_action_size)
	( ecsact_system_id  action_id
	);

/**
 * Get the amount of bytes a component with id `component_id` requies to 
 * serialize.
 */
ECSACT_SERIALIZE_API_FN(int, ecsact_serialize_component_size)
	( ecsact_component_id component_id
	);

/**
 * Serialize action into implementation defined format suitable for sending over
 * a socket and/or written to a file. Guranteed to be deserializable across
 * platforms.
 * 
 * @param action_id Valid action ID associated with `action_data`
 * @param in_action_data Valid action data associated with `action_id`
 * @param out_bytes Sequential byte array pointer that will be written to. The
 *        memory must be pre-allocated by the caller. The required size can be 
 *        queried for with `ecsact_serialize_action_size()`. The action ID is 
 *        not serialized.
 * @returns amount of bytes written to `out_bytes`.
 */
ECSACT_SERIALIZE_API_FN(int, ecsact_serialize_action)
	( ecsact_system_id  action_id
	, const void*       in_action_data
	, uint8_t*          out_bytes
	);

ECSACT_SERIALIZE_API_FN(int, ecsact_deserialize_action)
	( ecsact_system_id  action_id
	, const uint8_t*    in_bytes
	, void*             out_action_data
	);

ECSACT_SERIALIZE_API_FN(int, ecsact_serialize_component)
	( ecsact_component_id  component_id
	, const void*          in_component_data
	, uint8_t*             out_bytes
	);

ECSACT_SERIALIZE_API_FN(int, ecsact_deserialize_component)
	( ecsact_component_id  component_id
	, const uint8_t*       in_bytes
	, void*                out_component_data
	);

#define FOR_EACH_ECSACT_SERIALIZE_API_FN(fn, ...)\
	fn(ecsact_serialize_action_size, __VA_ARGS__);\
	fn(ecsact_serialize_component_size, __VA_ARGS__);\
	fn(ecsact_serialize_action, __VA_ARGS__);\
	fn(ecsact_serialize_component, __VA_ARGS__);\
	fn(ecsact_deserialize_action, __VA_ARGS__);\
	fn(ecsact_deserialize_component, __VA_ARGS__)

#undef ECSACT_SERIALIZE_API
#undef ECSACT_SERIALIZE_API_FN
#endif // ECSACT_RUNTIME_SERIALIZE_H
