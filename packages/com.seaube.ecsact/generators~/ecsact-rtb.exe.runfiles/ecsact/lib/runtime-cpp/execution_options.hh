#pragma once

#include <variant>
#include <vector>
#include <memory>
#include <boost/mp11.hpp>
#include <ecsact/lib.hh>
#include <ecsact/runtime/core.h>
#include <ecsact/runtime-cpp-c-interop.hh>

namespace ecsact {

	template<::ecsact::package Package>
	struct execution_options;

	template<::ecsact::package Package>
	struct prepared_execution_options {
		entity_id* add_components_entities;
		std::vector<ecsact_component> add_components;
		entity_id* update_components_entities;
		std::vector<ecsact_component> update_components;
		entity_id* remove_components_entities;
		int remove_components_length;
		component_id* remove_components;
		std::vector<ecsact_action> actions;

		ecsact_execution_options c() & {
			return ecsact_execution_options{
				.add_components_length = static_cast<int>(add_components.size()),
				.add_components_entities = reinterpret_cast<ecsact_entity_id*>(
					add_components_entities
				),
				.add_components = add_components.data(),
				.update_components_length = static_cast<int>(update_components.size()),
				.update_components_entities = reinterpret_cast<ecsact_entity_id*>(
					update_components_entities
				),
				.update_components = update_components.data(),
				.remove_components_length = remove_components_length,
				.remove_components_entities = reinterpret_cast<ecsact_entity_id*>(
					remove_components_entities
				),
				.remove_components = reinterpret_cast<ecsact_component_id*>(
					remove_components
				),
				.actions_length = static_cast<int>(actions.size()),
				.actions = actions.data(),
			};
		}
	};

	template<::ecsact::package Package>
	struct execution_options {
		using component_variant_type = boost::mp11::mp_apply
			< std::variant
			, typename Package::components
			>;
		using action_variant_type = boost::mp11::mp_apply
			< std::variant
			, typename Package::actions
			>;

		std::vector<entity_id> add_components_entities;
		std::vector<component_variant_type> add_components;
		std::vector<entity_id> update_components_entities;
		std::vector<component_variant_type> update_components;
		std::vector<entity_id> remove_components_entities;
		std::vector<component_id> remove_components;
		std::vector<action_variant_type> actions;

		prepared_execution_options<Package> prepare() {
			prepared_execution_options<Package> res;

			res.add_components_entities = add_components_entities.data();
			res.update_components_entities = update_components_entities.data();
			res.remove_components_entities = remove_components_entities.data();

			res.add_components.reserve(add_components.size());
			res.update_components.reserve(update_components.size());
			res.actions.reserve(actions.size());

			for(auto& comp_v : add_components) {
				std::visit([&]<typename C>(const C& comp) {
					res.add_components.push_back({
						.component_id = cppid_to_cid(C::id),
						.component_data = &comp,
					});
				}, comp_v);
			}

			for(auto& comp_v : update_components) {
				std::visit([&]<typename C>(const C& comp) {
					res.update_components.push_back({
						.component_id = cppid_to_cid(C::id),
						.component_data = &comp,
					});
				}, comp_v);
			}

			res.remove_components = remove_components.data();

			for(auto& action_v : actions) {
				std::visit([&]<typename A>(const A& action) {
					res.actions.push_back({
						.action_id = cppid_to_cid(static_cast<system_id>(A::id)),
						.action_data = &action,
					});
				}, action_v);
			}

			return res;
		}
	};

}
