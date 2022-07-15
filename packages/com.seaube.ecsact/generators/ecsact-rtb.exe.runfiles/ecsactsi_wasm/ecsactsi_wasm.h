/**
 * @file
 * @brief Ecsact System Implementation with WebAssembly header
 */

#ifndef ECSACTSI_WASM_H
#define ECSACTSI_WASM_H

#include <ecsact/runtime/common.h>

#ifndef ECSACTSI_WASM_API_VISIBILITY
#	ifdef ECSACTSI_WASM_API_LOAD_AT_RUNTIME
#		define ECSACTSI_WASM_API_VISIBILITY
#	else
#		ifdef ECSACTSI_WASM_API_EXPORT
#			ifdef _WIN32
#				define ECSACTSI_WASM_API_VISIBILITY __declspec(dllexport)
#			else
#				define ECSACTSI_WASM_API_VISIBILITY __attribute__((visibility("default")))
#			endif
#		else
#			ifdef _WIN32
#				define ECSACTSI_WASM_API_VISIBILITY __declspec(dllimport)
#			else
#				define ECSACTSI_WASM_API_VISIBILITY
#			endif
#		endif
#	endif
#endif // ECSACTSI_WASM_API_VISIBILITY

#ifndef ECSACTSI_WASM_API
#	ifdef __cplusplus
#		define ECSACTSI_WASM_API extern "C" ECSACTSI_WASM_API_VISIBILITY
#	else
#		define ECSACTSI_WASM_API extern ECSACTSI_WASM_API_VISIBILITY
# endif
#endif // ECSACTSI_WASM_API

#ifndef ECSACTSI_WASM_API_FN
#	ifdef ECSACTSI_WASM_API_LOAD_AT_RUNTIME
#		define ECSACTSI_WASM_API_FN(ret, name) ECSACTSI_WASM_API ret (*name)
#	else
#		define ECSACTSI_WASM_API_FN(ret, name) ECSACTSI_WASM_API ret name
#	endif
#endif // ECSACTSI_WASM_API_FN

typedef enum ecsactsi_wasm_error {
	/**
	 * No error.
	 */
	ECSACTSI_WASM_OK,

	/**
	 * Unable to open WASM file.
	 */
	ECSACTSI_WASM_ERR_FILE_OPEN_FAIL,

	/**
	 * Unable to read WASM file.
	 */
	ECSACTSI_WASM_ERR_FILE_READ_FAIL,

	/**
	 * Unable to compile WASM file module.
	 */
	ECSACTSI_WASM_ERR_COMPILE_FAIL,

	/**
	 * Unable to instantiate WASM file module.
	 */
	ECSACTSI_WASM_ERR_INSTANTIATE_FAIL,

	/**
	 * Export name was not found in WASM file.
	 */
	ECSACTSI_WASM_ERR_EXPORT_NOT_FOUND,

	/**
	 * Export name was found, but was not a function.
	 */
	ECSACTSI_WASM_ERR_EXPORT_INVALID,

	/**
	 * WASM file contains an unknown guest import.
	 */
	ECSACTSI_WASM_ERR_GUEST_IMPORT_UNKNOWN,

	/**
	 * WASM file contains correctly named guest import, but was not a function
	 */
	ECSACTSI_WASM_ERR_GUEST_IMPORT_INVALID,
} ecsactsi_wasm_error;

/**
 * Load WASM file at path `wasm_file_path` and call 
 * `ecsact_set_system_execution_impl` for the specified `system_ids` matching
 * the `wasm_exports` names.
 * @param wasm_file_path path to WASM file
 * @param systems_count Length of `system_ids` and `wasm_exports`
 * @param system_ids Sequential array of system ids that will have their system 
 *        implementations set to by the wasm exports dicated by `wasm_exports`
 *        in the same order. Length is determined by `systems_count`.
 * @param wasm_exports Sequential array of wasm export names used as system 
 *        implementations in the same order as `system_ids`. Length is
 *        determined by `systems_count`.
 * @return `ECSACTSI_WASM_OK` if no error. If there is an error for _any_ of the
 *         systems then **none of the systems are loaded**.
 */
ECSACTSI_WASM_API_FN(ecsactsi_wasm_error, ecsactsi_wasm_load_file)
	( const char*        wasm_file_path
	, int                systems_count
	, ecsact_system_id*  system_ids
	, const char**       wasm_exports
	);

ECSACTSI_WASM_API_FN(ecsactsi_wasm_error, ecsactsi_wasm_load)
	( char*              wasm_data
	, int                wasm_data_size
	, int                systems_count
	, ecsact_system_id*  system_ids
	, const char**       wasm_exports
	);

/**
 * @param system_id System ID associated with the impl that triggered the trap
 * @param trap_message The trap message contents. Null-terminated string.
 */
typedef void (*ecsactsi_wasm_trap_handler)
	( ecsact_system_id  system_id
	, const char*       trap_message
	);

/**
 * Register a function to be called when a system implementation trap occurs. It
 * is recommended that a trap handler is set otherwise the trap message will be
 * quietly discarded.
 * @param handler The handler function that will be called when a system impl
 *        function trap occurs. Calling this overwrites the last handler. May be
 *        `NULL` to remove the current handler.
 */
ECSACTSI_WASM_API_FN(void, ecsactsi_wasm_set_trap_handler)
	( ecsactsi_wasm_trap_handler handler
	);

#endif//ECSACTSI_WASM_H
