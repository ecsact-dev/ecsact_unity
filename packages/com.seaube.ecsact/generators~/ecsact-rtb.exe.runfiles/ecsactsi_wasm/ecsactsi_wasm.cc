#include "ecsactsi_wasm.h"

#include <map>
#include <iostream> // TODO(zaucy): remove this
#include <unordered_map>
#include <mutex>
#include <vector>
#include <string>
#include <string_view>
#include <shared_mutex>
#include <wasm.h>
#include <cstdlib>
#include <cstdio>
#include <functional>
#include <ecsact/runtime/dynamic.h>
#include <ecsact/runtime/meta.h>

#include "wasm_ecsact_system_execution.h"
#include "wasm_ecsact_pointer_map.hh"

using namespace std::string_literals;

namespace {
	struct wasm_system_module_info {
		wasm_module_t* system_module = {};
		wasm_instance_t* instance = {};
		const wasm_func_t* system_impl_func = {};
		wasm_memory_t* system_impl_memory = {};
		wasm_store_t* store = {};
	};

	// Utility to load wasm file which automatically cleans up upon destruction
	struct wasm_file_binary {
		wasm_byte_vec_t binary = {};

		static bool read(FILE* file, wasm_file_binary& out_file) {
			std::fseek(file, 0L, SEEK_END);
			auto file_size = std::ftell(file);
			std::fseek(file, 0L, SEEK_SET);
			wasm_byte_vec_new_uninitialized(&out_file.binary, file_size);

			if(std::fread(out_file.binary.data, file_size, 1, file) != 1) {
				return false;
			}

			return true;
		}

		~wasm_file_binary() {
			if(binary.data != nullptr) {
				wasm_byte_vec_delete(&binary);
				binary = {};
			}
		}
	};

	std::shared_mutex modules_mutex;
	std::map<ecsact_system_id, wasm_system_module_info> modules;
	ecsactsi_wasm_trap_handler trap_handler;

	using allowed_guest_imports_t = std::unordered_map
		< std::string
		, std::function<wasm_func_t*(wasm_store_t*)>
		>;

	const allowed_guest_imports_t allowed_guest_imports{
		{
			"ecsact_system_execution_context_action",
			[](wasm_store_t* store) -> wasm_func_t* {
				wasm_functype_t* fn_type = wasm_functype_new_1_1(
					wasm_valtype_new(WASM_ANYREF), // context
					wasm_valtype_new(WASM_ANYREF)  // action data (return)
				);
				wasm_func_t* fn = wasm_func_new(
					store,
					fn_type,
					&wasm_ecsact_system_execution_context_action
				);

				wasm_functype_delete(fn_type);

				return fn;
			},
		},
		{
			"ecsact_system_execution_context_get",
			[](wasm_store_t* store) -> wasm_func_t* {
				wasm_functype_t* fn_type = wasm_functype_new_3_0(
					wasm_valtype_new(WASM_I32),  // context
					wasm_valtype_new(WASM_I32),  // component_id
					wasm_valtype_new(WASM_I32)   // out_component_data
				);
				wasm_func_t* fn = wasm_func_new(
					store,
					fn_type,
					&wasm_ecsact_system_execution_context_get
				);

				wasm_functype_delete(fn_type);

				return fn;
			},
		},
		{
			"ecsact_system_execution_context_update",
			[](wasm_store_t* store) -> wasm_func_t* {
				wasm_functype_t* fn_type = wasm_functype_new_3_0(
					wasm_valtype_new(WASM_I32),  // context
					wasm_valtype_new(WASM_I32),  // component_id
					wasm_valtype_new(WASM_I32)   // component_data
				);
				wasm_func_t* fn = wasm_func_new(
					store,
					fn_type,
					&wasm_ecsact_system_execution_context_update
				);

				wasm_functype_delete(fn_type);

				return fn;
			},
		},
		{
			"ecsact_system_execution_context_add",
			[](wasm_store_t* store) -> wasm_func_t* {
				wasm_functype_t* fn_type = wasm_functype_new_3_0(
					wasm_valtype_new(WASM_I32),  // context
					wasm_valtype_new(WASM_I32),  // component_id
					wasm_valtype_new(WASM_I32)   // component_data
				);
				wasm_func_t* fn = wasm_func_new(
					store,
					fn_type,
					&wasm_ecsact_system_execution_context_add
				);

				wasm_functype_delete(fn_type);

				return fn;
			},
		},
		{
			"ecsact_system_execution_context_remove",
			[](wasm_store_t* store) -> wasm_func_t* {
				wasm_functype_t* fn_type = wasm_functype_new_2_0(
					wasm_valtype_new(WASM_I32),  // context
					wasm_valtype_new(WASM_I32)   // component_id
				);
				wasm_func_t* fn = wasm_func_new(
					store,
					fn_type,
					&wasm_ecsact_system_execution_context_remove
				);

				wasm_functype_delete(fn_type);

				return fn;
			},
		},
	};

	wasm_engine_t* engine() {
		static wasm_engine_t* engine = wasm_engine_new();
		return engine;
	}

	void ecsactsi_wasm_system_impl
		( ecsact_system_execution_context* ctx
		)
	{
		std::shared_lock lk(modules_mutex);
		auto system_id = ecsact_system_execution_context_id(ctx);
		auto& info = modules.at(system_id);

		set_wasm_ecsact_system_execution_context_memory(
			ctx,
			info.system_impl_memory
		);

		wasm_val_t as[1] = {{}};
		as[0].kind = WASM_I32;
		as[0].of.i32 = ecsactsi_wasm::as_guest_pointer(ctx);

		wasm_val_vec_t args = WASM_ARRAY_VEC(as);
		wasm_val_vec_t results = WASM_EMPTY_VEC;
		wasm_trap_t* trap = wasm_func_call(info.system_impl_func, &args, &results);

		if(trap_handler != nullptr && trap != nullptr) {
			wasm_message_t trap_msg;
			wasm_trap_message(trap, &trap_msg);
			std::string trap_msg_str{trap_msg.data, trap_msg.size};
			trap_handler(system_id, trap_msg_str.c_str());
		}

		set_wasm_ecsact_system_execution_context_memory(ctx, nullptr);
	}
}

ecsactsi_wasm_error ecsactsi_wasm_load
	( char*              wasm_data
	, int                wasm_data_size
	, int                systems_count
	, ecsact_system_id*  system_ids
	, const char**       wasm_exports
	)
{
	wasm_byte_vec_t binary{
		.size = static_cast<size_t>(wasm_data_size),
		.data = wasm_data,
	};

	decltype(modules) pending_modules;

	for(int index=0; systems_count > index; ++index) {
		auto system_id = system_ids[index];
		auto& pending_info = pending_modules[system_id];
		pending_info.store = wasm_store_new(engine());

		// There needs to be one module and one store per system for thread safety
		pending_info.system_module = wasm_module_new(
			pending_info.store,
			&binary
		);

		if(!pending_info.system_module) {
			return ECSACTSI_WASM_ERR_COMPILE_FAIL;
		}

		wasm_importtype_vec_t imports;
		wasm_exporttype_vec_t exports;
		wasm_module_imports(pending_info.system_module, &imports);
		wasm_module_exports(pending_info.system_module, &exports);
		int system_impl_export_memory_index = -1;
		int system_impl_export_function_index = -1;
		bool found_all_exports = false;

		std::cout << "exports.size=" << exports.size << "\n";

		for(size_t expi=0; exports.size > expi; ++expi) {
			auto export_name = wasm_exporttype_name(exports.data[expi]);
			auto export_type = wasm_exporttype_type(exports.data[expi]);
			auto export_type_kind = static_cast<wasm_externkind_enum>(
				wasm_externtype_kind(export_type)
			);

			if(export_type_kind == WASM_EXTERN_MEMORY) {
				system_impl_export_memory_index = expi;
			}

			std::string_view export_name_str(export_name->data, export_name->size);
			if(export_name_str == std::string_view(wasm_exports[index])) {
				std::cout << "export_name_str=" << export_name_str << "\n";
				if(export_type_kind != WASM_EXTERN_FUNC) {
					return ECSACTSI_WASM_ERR_EXPORT_INVALID;
				}

				system_impl_export_function_index = expi;
			}

			found_all_exports =
				system_impl_export_memory_index != -1 &&
				system_impl_export_function_index != -1;

			if(found_all_exports) break;
		}

		if(system_impl_export_function_index == -1) {
			return ECSACTSI_WASM_ERR_EXPORT_NOT_FOUND;
		}

		std::vector<wasm_extern_t*> externs;
		externs.reserve(std::min(static_cast<size_t>(8), imports.size));
		for(size_t impi=0; imports.size > impi; ++impi) {
			auto import_name = wasm_importtype_name(imports.data[impi]);
			auto import_type = wasm_importtype_type(imports.data[impi]);
			auto import_type_kind = static_cast<wasm_externkind_enum>(
				wasm_externtype_kind(import_type)
			);
			
			std::string import_name_str(import_name->data, import_name->size);

			if(!allowed_guest_imports.contains(import_name_str)) {
				return ECSACTSI_WASM_ERR_GUEST_IMPORT_UNKNOWN;
			}

			if(import_type_kind != WASM_EXTERN_FUNC) {
				return ECSACTSI_WASM_ERR_GUEST_IMPORT_INVALID;
			}

			auto guest_import_fn =
				allowed_guest_imports.at(import_name_str)(pending_info.store);
			externs.push_back(wasm_func_as_extern(guest_import_fn));
			
			// TODO(zaucy): Determine if we need to delete function here or later
			// wasm_func_delete(guest_import_fn);
		}

		wasm_extern_vec_t instance_externs{
			.size = externs.size(),
			.data = externs.data(),
		};

		pending_info.instance = wasm_instance_new(
			pending_info.store,
			pending_info.system_module,
			&instance_externs,
			nullptr
		);

		if(!pending_info.instance) {
			return ECSACTSI_WASM_ERR_INSTANTIATE_FAIL;
		}

		wasm_extern_vec_t inst_exports;
		wasm_instance_exports(pending_info.instance, &inst_exports);

		{
			auto fn_extern = inst_exports.data[system_impl_export_function_index];
			pending_info.system_impl_func = wasm_extern_as_func(fn_extern);
		}

		if(system_impl_export_memory_index != -1) {
			assert(inst_exports.size > system_impl_export_memory_index);
			auto mem_extern = inst_exports.data[system_impl_export_memory_index];
			pending_info.system_impl_memory = wasm_extern_as_memory(mem_extern);
		}
	}

	{
		std::unique_lock lk(modules_mutex);
		for(auto&& [system_id, pending_info] : pending_modules) {
			if(modules.contains(system_id)) {
				// TODO(zaucy): Cleanup existing module info
			}

			modules[system_id] = std::move(pending_info);
			ecsact_set_system_execution_impl(system_id, &ecsactsi_wasm_system_impl);
		}
	}

	return ECSACTSI_WASM_OK;
}

ecsactsi_wasm_error ecsactsi_wasm_load_file
	( const char*        wasm_file_path
	, int                systems_count
	, ecsact_system_id*  system_ids
	, const char**       wasm_exports
	)
{
	FILE* file = std::fopen(wasm_file_path, "rb");
	if(!file) {
		return ECSACTSI_WASM_ERR_FILE_OPEN_FAIL;
	}

	wasm_file_binary file_bin;
	{
		const bool read_success = wasm_file_binary::read(file, file_bin);
		std::fclose(file);
		if(!read_success) {
			return ECSACTSI_WASM_ERR_FILE_READ_FAIL;
		}
	}

	return ecsactsi_wasm_load(
		file_bin.binary.data,
		file_bin.binary.size,
		systems_count,
		system_ids,
		wasm_exports
	);
}

void ecsactsi_wasm_set_trap_handler
	( ecsactsi_wasm_trap_handler handler
	)
{
	std::shared_lock lk(modules_mutex);
	trap_handler = handler;
}
