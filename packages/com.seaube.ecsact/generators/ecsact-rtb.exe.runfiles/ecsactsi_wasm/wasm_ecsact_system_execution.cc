#include "wasm_ecsact_system_execution.h"

#include <cassert>
#include <iostream>
#include <unordered_map>
#include <ecsact/runtime/dynamic.h>

#include "wasm_ecsact_pointer_map.hh"

namespace {
	std::unordered_map<ecsact_system_execution_context*, wasm_memory_t*> mem_map;

	ecsact_system_execution_context* get_execution_context
		( const wasm_val_t& val
		)
	{
		assert(val.kind == WASM_I32);
		return ecsactsi_wasm::as_host_pointer<ecsact_system_execution_context>(
			val.of.i32
		);
	}

	ecsact_component_id get_component_id
		( const wasm_val_t&  val
		)
	{
		assert(val.kind == WASM_I32);
		return static_cast<ecsact_component_id>(val.of.i32);
	}

	void* get_void_ptr
		( const wasm_val_t&  val
		, wasm_memory_t*     memory
		)
	{
		assert(val.kind == WASM_I32);
		if(val.of.i32 == 0) return nullptr;
		
		auto mem_bytes = wasm_memory_data(memory);
		// the i32 val is an index of the wasm memory data
		return mem_bytes + val.of.i32;
	}

	const void* get_const_void_ptr
		( const wasm_val_t&  val
		, wasm_memory_t*     memory
		)
	{
		assert(val.kind == WASM_I32);
		if(val.of.i32 == 0) return nullptr;

		auto mem_bytes = wasm_memory_data(memory);
		// the i32 val is an index of the wasm memory data
		return mem_bytes + val.of.i32;
	}
}

void set_wasm_ecsact_system_execution_context_memory
	( ecsact_system_execution_context*  ctx
	, wasm_memory_t*                    memory
	)
{
	if(memory == nullptr) {
		mem_map.erase(ctx);
	} else {
		mem_map[ctx] = memory;
	}
}

wasm_trap_t* wasm_ecsact_system_execution_context_action
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	// TODO(zaucy): unimplemented

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_add
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	auto ctx = get_execution_context(args->data[0]);
	auto memory = mem_map.at(ctx);

	ecsact_system_execution_context_add(
		ctx,
		get_component_id(args->data[1]),
		get_const_void_ptr(args->data[2], memory)
	);

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_remove
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	ecsact_system_execution_context_remove(
		get_execution_context(args->data[0]),
		get_component_id(args->data[1])
	);
	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_get
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	auto ctx = get_execution_context(args->data[0]);
	auto memory = mem_map.at(ctx);

	ecsact_system_execution_context_get(
		ctx,
		get_component_id(args->data[1]),
		get_void_ptr(args->data[2], memory)
	);

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_update
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	auto ctx = get_execution_context(args->data[0]);
	auto memory = mem_map.at(ctx);

	ecsact_system_execution_context_update(
		ctx,
		get_component_id(args->data[1]),
		get_const_void_ptr(args->data[2], memory)
	);

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_has
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	bool has_component = ecsact_system_execution_context_has(
		get_execution_context(args->data[0]),
		get_component_id(args->data[1])
	);

	results->data[0].kind = WASM_I32;
  results->data[0].of.i32 = has_component ? 1 : 0;

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_generate
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	// ecsact_system_execution_context_generate(
	// 	get_execution_context(args->data[0]),
	// );
	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_parent
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	auto parent = ecsact_system_execution_context_parent(
		get_execution_context(args->data[0])
	);

	return nullptr;
}

wasm_trap_t* wasm_ecsact_system_execution_context_same
	( const wasm_val_vec_t*  args
	, wasm_val_vec_t*        results
	)
{
	bool same = ecsact_system_execution_context_same(
		get_execution_context(args->data[0]),
		get_execution_context(args->data[1])
	);
  
	results->data[0].kind = WASM_I32;
  results->data[0].of.i32 = same ? 1 : 0;

	return nullptr;
}
