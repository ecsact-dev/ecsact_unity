#pragma once

#include <type_traits>

namespace ecsact::entt {
	/**
	 * Marker to indicate that a component has been added during execution
	 */
	template<typename C>
	struct component_added {};

	/**
	 * Marker to indicate that a component has been changed during execution
	 */
	template<typename C>
	struct component_changed {};

	/**
	 * Marker to indicate that a component has been removed
	 */
	template<typename C>
	struct component_removed {};
}

namespace ecsact::entt::detail {
	template<typename C>
	struct temp_storage;

	template<typename C> requires(std::is_empty_v<C>)
	struct temp_storage<C> { };

	template<typename C> requires(!std::is_empty_v<C>)
	struct temp_storage<C> { C value; };

	template<typename C> requires(!std::is_empty_v<C>)
	struct beforechange_storage { C value; bool set = false; };

	template<typename C>
	struct pending_add;

	template<typename C> requires(std::is_empty_v<C>)
	struct pending_add<C> { };

	template<typename C> requires(!std::is_empty_v<C>)
	struct pending_add<C> { C value; };

	template<typename C>
	struct pending_remove {};
}
