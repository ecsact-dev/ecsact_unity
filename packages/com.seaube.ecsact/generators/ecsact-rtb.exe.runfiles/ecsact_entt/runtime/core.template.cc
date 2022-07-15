#include <ecsact/runtime/core.h>

#include "common.template.hh"

using namespace ecsact_entt_rt;

ecsact_registry_id ecsact_create_registry
	( const char* registry_name
	)
{
	return static_cast<ecsact_registry_id>(
		runtime.create_registry(registry_name)
	);
}

void ecsact_destroy_registry
	( ecsact_registry_id reg_id
	)
{
	runtime.destroy_registry(static_cast<ecsact::registry_id>(reg_id));
}

void ecsact_clear_registry
	( ecsact_registry_id reg_id
	)
{
	runtime.clear_registry(static_cast<ecsact::registry_id>(reg_id));
}

ecsact_entity_id ecsact_create_entity
	( ecsact_registry_id reg_id
	)
{
	return static_cast<ecsact_entity_id>(
		runtime.create_entity(static_cast<ecsact::registry_id>(reg_id))
	);
}

void ecsact_ensure_entity
	( ecsact_registry_id  reg_id
	, ecsact_entity_id    entity_id
	)
{
	runtime.ensure_entity(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id)
	);
}

bool ecsact_entity_exists
	( ecsact_registry_id  reg_id
	, ecsact_entity_id    entity_id
	)
{
	return runtime.entity_exists(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id)
	);
}

void ecsact_destroy_entity
	( ecsact_registry_id  reg_id
	, ecsact_entity_id    entity_id
	)
{
	runtime.destroy_entity(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id)
	);
}

int ecsact_count_entities
	( ecsact_registry_id  reg_id
	)
{
	return runtime.count_entities(static_cast<ecsact::registry_id>(reg_id));
}

void ecsact_get_entities
	( ecsact_registry_id  reg_id
	, int                 max_entities_count
	, ecsact_entity_id*   out_entities
	, int*                out_entities_count
	)
{
	runtime.get_entities(
		static_cast<ecsact::registry_id>(reg_id),
		max_entities_count,
		reinterpret_cast<ecsact::entity_id*>(out_entities),
		out_entities_count
	);
}

void ecsact_add_component
	( ecsact_registry_id   reg_id
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	, const void*          component_data
	)
{
	runtime.add_component(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id),
		static_cast<ecsact::component_id>(component_id),
		component_data
	);
}

bool ecsact_has_component
	( ecsact_registry_id   reg_id
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	)
{
	return runtime.has_component(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id),
		static_cast<ecsact::component_id>(component_id)
	);
}

const void* ecsact_get_component
	( ecsact_registry_id   reg_id
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	)
{
	return runtime.get_component(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id),
		static_cast<ecsact::component_id>(component_id)
	);
}

int ecsact_count_components
	( ecsact_registry_id     registry_id
	, ecsact_entity_id       entity_id
	)
{
	return runtime.count_components(
		static_cast<ecsact::registry_id>(registry_id),
		static_cast<ecsact::entity_id>(entity_id)
	);
}

void ecsact_each_component
	( ecsact_registry_id              registry_id
	, ecsact_entity_id                entity_id
	, ecsact_each_component_callback  callback
	, void*                           callback_user_data
	)
{
	runtime.each_component(
		static_cast<ecsact::registry_id>(registry_id),
		static_cast<ecsact::entity_id>(entity_id),
		callback,
		callback_user_data
	);
}

void ecsact_get_components
	( ecsact_registry_id     registry_id
	, ecsact_entity_id       entity_id
	, int                    max_components_count
	, ecsact_component_id*   out_component_ids
	, const void**           out_components_data
	, int*                   out_components_count
	)
{
	runtime.get_components(
		static_cast<ecsact::registry_id>(registry_id),
		static_cast<ecsact::entity_id>(entity_id),
		max_components_count,
		reinterpret_cast<ecsact::component_id*>(out_component_ids),
		out_components_data,
		out_components_count
	);
}

void ecsact_update_component
	( ecsact_registry_id   reg_id
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	, const void*          component_data
	)
{
	runtime.update_component(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id),
		static_cast<ecsact::component_id>(component_id),
		component_data
	);
}

void ecsact_remove_component
	( ecsact_registry_id   reg_id
	, ecsact_entity_id     entity_id
	, ecsact_component_id  component_id
	)
{
	runtime.remove_component(
		static_cast<ecsact::registry_id>(reg_id),
		static_cast<ecsact::entity_id>(entity_id),
		static_cast<ecsact::component_id>(component_id)
	);
}

void ecsact_execute_systems
	( ecsact_registry_id                        registry_id
	, int                                       execution_count
	, const ecsact_execution_options*           execution_options_list
	, const ecsact_execution_events_collector*  c_events_collector
	)
{
	using ecsact_entt_rt::execution_events_collector;

	std::optional<execution_events_collector> events_collector_opt = {};
	if(c_events_collector != nullptr) {
		auto& events_collector = events_collector_opt.emplace();
		events_collector.target = c_events_collector;
	}

	runtime.execute_systems(
		static_cast<ecsact::registry_id>(registry_id),
		execution_count,
		execution_options_list,
		events_collector_opt
	);
}
