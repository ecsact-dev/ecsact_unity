#pragma once

#include <cstdint>
#include <vector>
#include <unordered_map>
#include <type_traits>
#include <functional>
#include <concepts>

// Some forwards from <ecsact/runtime/core.h>
struct ecsact_execution_options;
struct ecsact_execution_events_collector;

namespace ecsact::detail {
	struct system_execution_context;
}

namespace ecsact {
	enum class registry_id : std::int32_t;
	enum class component_id : std::int32_t;
	enum class variant_id : std::int32_t;
	enum class system_id : std::int32_t;
	enum class action_id : std::int32_t;
	enum class entity_id : std::int32_t;

	class registry {
		using component_callbacks_container_t = std::unordered_map
			< component_id
			, std::vector<std::function<void(entity_id, const void*)>>
			>;
		using component_no_data_callbacks_container_t = std::unordered_map
			< component_id
			, std::vector<std::function<void(entity_id)>>
			>;

		registry_id id_;

	public:
		/**
		 * Creates registry by calling `ecsact_create_registry()`
		 */
		registry();

		/**
		 * Create C++ wrapper around an already created registry from the C API
		 */
		explicit registry
			( registry_id id
			);

		registry(registry&& other);

		registry(const registry&) = delete;

		/**
		 * Calls `ecsact_destroy_registry()` if id is valid
		 */
		~registry();

		registry_id id() const noexcept;

		/**
		 * Calls `ecsact_clear_registry()`
		 */
		void clear();

		/**
		 * Calls `ecsact_create_entity()`
		 */
		entity_id create_entity();

		/**
		 * Calls `ecsact_ensure_entity()`
		 */
		void ensure_entity
			( entity_id
			);

		/**
		 * Calls `ecsact_entity_exists`
		 */
		bool entity_exists
			( entity_id
			) const;

		/**
		 * Calls `ecsact_destroy_entity()`
		 */
		void destroy_entity
			( entity_id
			);

		/**
		 * Calls `ecsact_count_entities`
		 */
		int count_entities();

		/**
		 * Calls `ecsact_get_entities()`
		 */
		std::vector<entity_id> get_entities();

		/**
		 * Calls `ecsact_add_component()`
		 */
		void add_component
			( entity_id     entity_id
			, component_id  component_id
			, const void*   component_data
			);

		template<typename ComponentT>
		void add_component
			( entity_id          entity_id
			, const ComponentT&  component
			)
		{
			if constexpr(std::is_empty_v<ComponentT>) {
				add_component(entity_id, ComponentT::id, nullptr);
			} else {
				add_component(
					entity_id,
					ComponentT::id,
					static_cast<const void*>(&component)
				);
			}
		}

		template<typename ComponentT>
			requires(std::is_empty_v<ComponentT>)
		void add_component
			( entity_id entity_id
			)
		{
			add_component(entity_id, ComponentT::id, nullptr);
		}

		bool has_component
			( entity_id     entity_id
			, component_id  component_id
			);

		/**
		 * Calls `ecsact_has_component()`
		 */
		template<typename ComponentT>
		bool has_component
			( entity_id entity_id
			)
		{
			return has_component(entity_id, ComponentT::id);
		}

		/**
		 * Calls `ecsact_get_component()`
		 */
		const void* get_component
			( entity_id     entity_id
			, component_id  component_id
			);

		template<typename ComponentT>
		const ComponentT& get_component
			( entity_id entity_id
			)
		{
			return *static_cast<const ComponentT*>(
				get_component(entity_id, ComponentT::id)
			);
		}

		/**
		 * Calls `ecsact_count_components`
		 */
		int count_components
			( entity_id entity_id
			);

		/**
		 * Get all components for an entity
		 */
		std::unordered_map<component_id, const void*> get_components
			( entity_id entity_id
			);

		/**
		 * Calls `ecsact_each_component`
		 */
		void each_component
			( entity_id                                       entity_id
			, std::function<void(component_id, const void*)>  callback
			);

		/**
		 * Calls `ecsact_update_component()`
		 */
		void update_component
			( entity_id     entity_id
			, component_id  component_id
			, const void*   component_data
			);

		template<typename ComponentT>
			requires(!std::is_empty_v<ComponentT>)
		void update_component
			( entity_id          entity_id
			, const ComponentT&  component
			)
		{
			update_component(
				entity_id,
				ComponentT::id,
				static_cast<const void*>(&component)
			);
		}

		/**
		 * Calls `ecsact_remove_component()`
		 */
		void remove_component
			( entity_id     entity_id
			, component_id  component_id
			);

		template<typename ComponentT>
		void remove_component
			( entity_id entity_id
			)
		{
			return remove_component(entity_id, ComponentT::id);
		}

		/**
		 * Calls `ecsact_execute_systems()`
		 */
		void execute_systems
			( int                                       execution_count = 1
			, const ecsact_execution_options*           exec_options = nullptr
			, const ecsact_execution_events_collector*  events_collector = nullptr
			);

		/**
		 * Convenience overload
		 */
		inline void execute_systems
			( int                                       execution_count
			, const ecsact_execution_options&           exec_options
			, const ecsact_execution_events_collector&  events_collector
			)
		{
			execute_systems(execution_count, &exec_options, &events_collector);
		}

		/**
		 * Convenience overload
		 */
		inline void execute_systems
			( int                                       execution_count
			, const ecsact_execution_options&           exec_options
			)
		{
			execute_systems(execution_count, &exec_options, nullptr);
		}

		/**
		 * Convenience overload
		 */
		inline void execute_systems
			( int                                       execution_count
			, const ecsact_execution_events_collector&  events_collector
			)
		{
			execute_systems(execution_count, nullptr, &events_collector);
		}
	};
}
