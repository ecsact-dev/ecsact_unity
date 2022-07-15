/**
 * @file
 * @internal
 */

#ifndef WASM_ECSACT_SYSTEM_EXECUTION__H
#define WASM_ECSACT_SYSTEM_EXECUTION__H

#include <wasm.h>
#include <ecsact/runtime/common.h>

void set_wasm_ecsact_system_execution_context_memory
	( ecsact_system_execution_context*  ctx
	, wasm_memory_t*                    memory
	);

wasm_trap_t* wasm_ecsact_system_execution_context_action
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_add
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_remove
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_get
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_update
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_has
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_generate
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_parent
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

wasm_trap_t* wasm_ecsact_system_execution_context_same
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	);

#endif//WASM_ECSACT_SYSTEM_EXECUTION__H
