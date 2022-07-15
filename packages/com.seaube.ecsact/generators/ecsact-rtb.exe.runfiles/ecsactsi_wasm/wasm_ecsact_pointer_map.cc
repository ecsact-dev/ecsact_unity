#include "wasm_ecsact_pointer_map.hh"

#include <map>

namespace {
	std::int32_t last_guest_pointer_id{};
	std::map<std::int32_t, void*> guest_to_host_ptr_map({{0, nullptr}});
	std::map<void*, std::int32_t> host_to_guest_ptr_map({{nullptr, 0}});

	std::int32_t new_guest_pointer_id() {
		last_guest_pointer_id += 1;
		return last_guest_pointer_id;
	}
}

void* ecsactsi_wasm::as_host_pointer
	( std::int32_t guest_pointer_id
	)
{
	if(guest_to_host_ptr_map.contains(guest_pointer_id)) {
		return guest_to_host_ptr_map.at(guest_pointer_id);
	}

	return 0;
}

std::int32_t ecsactsi_wasm::as_guest_pointer
	( void* host_pointer
	)
{
	if(host_to_guest_ptr_map.contains(host_pointer)) {
		return host_to_guest_ptr_map.at(host_pointer);
	}

	std::int32_t guest_pointer_id = new_guest_pointer_id();
	guest_to_host_ptr_map[guest_pointer_id] = host_pointer;
	host_to_guest_ptr_map[host_pointer] = guest_pointer_id;

	return guest_pointer_id;
}

void ecsactsi_wasm::free_host_guest_pointer
	( void* host_pointer
	)
{
	if(host_pointer != 0) {
		if(host_to_guest_ptr_map.contains(host_pointer)) {
			guest_to_host_ptr_map.erase(host_to_guest_ptr_map.at(host_pointer));
			host_to_guest_ptr_map.erase(host_pointer);
		}
	}
}

void ecsactsi_wasm::free_all_host_guest_pointers() {
	guest_to_host_ptr_map.erase(
		std::next(guest_to_host_ptr_map.begin()),
		guest_to_host_ptr_map.end()
	);
	host_to_guest_ptr_map.erase(
		std::next(host_to_guest_ptr_map.begin()),
		host_to_guest_ptr_map.end()
	);
}
