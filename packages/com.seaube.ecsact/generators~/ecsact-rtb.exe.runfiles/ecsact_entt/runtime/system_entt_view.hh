#pragma once

#include <boost/mp11.hpp>
#include <entt/entt.hpp>

#include "event_markers.hh"

namespace ecsact::entt::detail {
	template<typename... C, typename... E>
	auto system_view_helper
		( boost::mp11::mp_list<C...>
		, boost::mp11::mp_list<E...>
		, ::entt::registry& registry
		)
	{
		return registry.view<C...>(::entt::exclude<E...>);
	}
}

namespace ecsact::entt {
	template<typename SystemT>
	auto system_view
		( ::entt::registry& registry
		)
	{
		using boost::mp11::mp_unique;
		using boost::mp11::mp_push_back;
		using boost::mp11::mp_flatten;
		using boost::mp11::mp_list;
		using boost::mp11::mp_push_back;
		using boost::mp11::mp_assign;
		using boost::mp11::mp_transform;

		using ecsact::entt::detail::temp_storage;
		using ecsact::entt::detail::beforechange_storage;

		using get_types = mp_assign<mp_list<>, mp_unique<mp_flatten<mp_push_back<
			typename SystemT::writables,
			typename SystemT::readables,
			typename SystemT::includes,
			typename SystemT::removes,
			mp_transform<beforechange_storage, typename SystemT::writables>
		>>>>;

		// using t = get_types::todo_remove_this;

		using exclude_types = mp_assign<
			mp_list<>,
			mp_unique<mp_flatten<mp_push_back<
				typename SystemT::excludes,
				typename SystemT::adds
			>>>
		>;

		return detail::system_view_helper(get_types{}, exclude_types{}, registry);
	}

	template<typename SystemT>
	using system_view_type = decltype(
		system_view<SystemT>(std::declval<::entt::registry&>())
	);
}
