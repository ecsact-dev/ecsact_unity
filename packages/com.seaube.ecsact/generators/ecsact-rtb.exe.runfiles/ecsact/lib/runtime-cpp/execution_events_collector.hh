#pragma once

#include <functional>
#include <variant>
#include <ecsact/lib.hh>
#include <ecsact/component_variant.hh>
#include <ecsact/runtime/core.h>
#include <ecsact/runtime-cpp-c-interop.hh>

namespace ecsact {
	struct execution_events_collector {
		using callback_type =
			std::function<void(entity_id, any_component)>;

		/**
		 * Helper function to call the std::function from the callback user data
		 */
		static void invoke_cpp_callback
			( ecsact_event
			, ecsact_entity_id     entity_id
			, ecsact_component_id  component_id
			, const void*          component_data
			, void*                callback_user_data
			)
		{
			auto& callback = *static_cast<const callback_type*>(callback_user_data);

			callback(
				cid_to_cppid(entity_id),
				any_component(cid_to_cppid(component_id), component_data)
			);
		}

		/**
		 * SEE: `ecsact_execution_events_collector::init_callback`
		 */
		callback_type init_callback;

		/**
		 * SEE: `ecsact_execution_events_collector::update_callback`
		 */
		callback_type update_callback;

		/**
		 * SEE: `ecsact_execution_events_collector::remove_callback`
		 */
		callback_type remove_callback;
		
		/**
		 * Get C compatible execution events collector
		 */
		ecsact_execution_events_collector c() {
			return {
				.init_callback = &invoke_cpp_callback,
				.init_callback_user_data = static_cast<void*>(&init_callback),
				.update_callback = &invoke_cpp_callback,
				.update_callback_user_data = static_cast<void*>(&update_callback),
				.remove_callback = &invoke_cpp_callback,
				.remove_callback_user_data = static_cast<void*>(&remove_callback),
			};
		}
	};
}
