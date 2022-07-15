#include <ecsact/runtime.hh>

#include <string>
#include <ecsact/runtime/core.h>

using ecsact::registry;
using ecsact::component_id;


ecsact::registry::registry()
	: registry(static_cast<registry_id>(ecsact_create_registry("C++ Runtime")))
{
}

ecsact::registry::registry
	( registry_id existing_id
	)
	: id_(existing_id)
{
}

ecsact::registry::registry
	( registry&& other
	)
	: id_(other.id_)
{
	other.id_ = static_cast<registry_id>(ecsact_invalid_registry_id);
}

ecsact::registry::~registry() {
	if(static_cast<ecsact_registry_id>(id_) != ecsact_invalid_registry_id) {
		ecsact_destroy_registry(static_cast<ecsact_registry_id>(id_));
		id_ = static_cast<registry_id>(ecsact_invalid_registry_id);
	}
}

ecsact::registry_id ecsact::registry::id() const noexcept {
	return id_;
}

void ecsact::registry::clear() {
	ecsact_clear_registry(static_cast<ecsact_registry_id>(id_));
}

ecsact::entity_id ecsact::registry::create_entity() {
	return static_cast<ecsact::entity_id>(
		ecsact_create_entity(static_cast<ecsact_registry_id>(id_))
	);
}

void ecsact::registry::ensure_entity
	( ecsact::entity_id entity_id
	)
{
	ecsact_ensure_entity(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id)
	);
}

bool ecsact::registry::entity_exists
	( entity_id entity_id
	) const
{
	return ecsact_entity_exists(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id)
	);
}

void ecsact::registry::destroy_entity
	( entity_id entity_id
	)
{
	ecsact_destroy_entity(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id)
	);
}

int ecsact::registry::count_entities() {
	return ecsact_count_entities(static_cast<ecsact_registry_id>(id_));
}

std::vector<ecsact::entity_id> ecsact::registry::get_entities() {
	std::vector<ecsact::entity_id> entities;
	entities.resize(count_entities());
	ecsact_get_entities(
		static_cast<ecsact_registry_id>(id_),
		entities.size(),
		reinterpret_cast<ecsact_entity_id*>(entities.data()),
		nullptr
	);

	return entities;
}

void ecsact::registry::add_component
	( entity_id     entity_id
	, component_id  component_id
	, const void*   component_data
	)
{
	ecsact_add_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		static_cast<ecsact_component_id>(component_id),
		component_data
	);
}

bool ecsact::registry::has_component
	( entity_id     entity_id
	, component_id  component_id
	)
{
	return ecsact_has_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		static_cast<ecsact_component_id>(component_id)
	);
}

const void* ecsact::registry::get_component
	( entity_id     entity_id
	, component_id  component_id
	)
{
	return ecsact_get_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		static_cast<ecsact_component_id>(component_id)
	);
}

int ecsact::registry::count_components
	( entity_id entity_id
	)
{
	return ecsact_count_components(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id)
	);
}

std::unordered_map<component_id, const void*> registry::get_components
	( entity_id entity_id
	)
{
	int count = count_components(entity_id);
	std::unordered_map<component_id, const void*> components;
	std::vector<ecsact_component_id> component_ids;
	std::vector<const void*> component_datas;
	components.reserve(count);
	component_ids.resize(count);
	component_datas.resize(count);
	ecsact_get_components(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		count,
		component_ids.data(),
		component_datas.data(),
		nullptr
	);

	for(int i=0; count > i; ++i) {
		components.insert({
			static_cast<component_id>(component_ids[i]),
			component_datas[i]
		});
	}

	return components;
}

static void each_component_callback
	( ecsact_component_id  component_id
	, const void*          component_data
	, void*                user_data
	)
{
	using fn_type = std::function<void(ecsact::component_id, const void*)>;
	auto& callback = *reinterpret_cast<fn_type*>(user_data);
	callback(
		static_cast<ecsact::component_id>(component_id),
		component_data
	);
}

void ecsact::registry::each_component
	( entity_id                                       entity_id
	, std::function<void(component_id, const void*)>  callback
	)
{
	ecsact_each_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		&each_component_callback,
		&callback
	);
}

void ecsact::registry::update_component
	( entity_id     entity_id
	, component_id  component_id
	, const void*   component_data
	)
{
	ecsact_update_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		static_cast<ecsact_component_id>(component_id),
		component_data
	);
}

void ecsact::registry::remove_component
	( entity_id     entity_id
	, component_id  component_id
	)
{
	ecsact_remove_component(
		static_cast<ecsact_registry_id>(id_),
		static_cast<ecsact_entity_id>(entity_id),
		static_cast<ecsact_component_id>(component_id)
	);
}

void ecsact::registry::execute_systems
	( int                                       execution_count
	, const ecsact_execution_options*           exec_options
	, const ecsact_execution_events_collector*  events_collector
	)
{
	ecsact_execute_systems(
		static_cast<ecsact_registry_id>(id_),
		execution_count,
		exec_options,
		events_collector
	);
}
