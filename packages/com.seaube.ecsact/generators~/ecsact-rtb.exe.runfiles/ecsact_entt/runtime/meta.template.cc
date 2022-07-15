#include <ecsact/runtime/meta.h>

#include "common.template.hh"

using namespace ecsact_entt_rt;

size_t ecsact_meta_component_size
	( ecsact_component_id comp_id
	)
{
	return runtime.component_size(static_cast<ecsact::component_id>(comp_id));
}

size_t ecsact_meta_action_size
	( ecsact_system_id action_id
	)
{
	return runtime.action_size(static_cast<ecsact::action_id>(action_id));
}
