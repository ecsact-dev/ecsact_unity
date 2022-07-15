/**
 * @file
 * @internal
 */

#pragma once

#include <map>
#include <cstdint>
#include <unordered_set>

namespace ecsactsi_wasm {
	void* as_host_pointer
		( std::int32_t guest_pointer_id
		);

	template<typename T>
	T* as_host_pointer
		( std::int32_t guest_pointer_id
		)
	{
		return (T*)as_host_pointer(guest_pointer_id);
	}
	
	std::int32_t as_guest_pointer
		( void* host_pointer
		);

	void free_host_guest_pointer
		( void* host_pointer
		);

	void free_all_host_guest_pointers();
}
