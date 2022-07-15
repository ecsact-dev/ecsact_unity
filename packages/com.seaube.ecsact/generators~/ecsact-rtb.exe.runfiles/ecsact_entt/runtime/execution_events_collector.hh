#pragma once

#include <type_traits>
#include <ecsact/runtime/core.h>
#include <ecsact/runtime.hh>

namespace ecsact_entt_rt {
	struct execution_events_collector {
		const ecsact_execution_events_collector* target;

		inline bool has_init_callback() const {
			return target->init_callback != nullptr;
		}

		inline bool has_update_callback() const {
			return target->update_callback != nullptr;
		}

		inline bool has_remove_callback() const {
			return target->remove_callback != nullptr;
		}

		template<typename C>
			requires(!std::is_empty_v<C>)
		void invoke_init_callback
			( ::ecsact::entity_id  entity
			, const C&             component
			)
		{
			target->init_callback(
				ECSACT_EVENT_INIT_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				static_cast<const void*>(&component),
				target->init_callback_user_data
			);
		}

		template<typename C>
			requires(std::is_empty_v<C>)
		void invoke_init_callback
			( ::ecsact::entity_id entity
			)
		{
			target->init_callback(
				ECSACT_EVENT_INIT_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				nullptr,
				target->init_callback_user_data
			);
		}

		template<typename C>
			requires(!std::is_empty_v<C>)
		void invoke_update_callback
			( ::ecsact::entity_id  entity
			, const C&             component
			)
		{
			target->update_callback(
				ECSACT_EVENT_UPDATE_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				static_cast<const void*>(&component),
				target->update_callback_user_data
			);
		}

		template<typename C>
			requires(std::is_empty_v<C>)
		void invoke_update_callback
			( ::ecsact::entity_id entity
			)
		{
			target->update_callback(
				ECSACT_EVENT_UPDATE_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				nullptr,
				target->update_callback_user_data
			);
		}

		template<typename C>
			requires(!std::is_empty_v<C>)
		void invoke_remove_callback
			( ::ecsact::entity_id  entity
			, const C&             component
			)
		{
			target->remove_callback(
				ECSACT_EVENT_REMOVE_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				static_cast<const void*>(&component),
				target->remove_callback_user_data
			);
		}

		template<typename C>
			requires(std::is_empty_v<C>)
		void invoke_remove_callback
			( ::ecsact::entity_id entity
			)
		{
			target->remove_callback(
				ECSACT_EVENT_REMOVE_COMPONENT,
				static_cast<ecsact_entity_id>(entity),
				static_cast<ecsact_component_id>(C::id),
				nullptr,
				target->remove_callback_user_data
			);
		}
	};
}
