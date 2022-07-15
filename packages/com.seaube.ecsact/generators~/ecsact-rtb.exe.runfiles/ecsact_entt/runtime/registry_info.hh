#pragma once

#include <type_traits>
#include <optional>
#include <utility>
#include <mutex>
#include <boost/mp11.hpp>
#include <entt/entt.hpp>
#include <ecsact/runtime.hh>
#include <ecsact/runtime/common.h>
#include <ecsact/runtime/core.h>

#include "event_markers.hh"

namespace ecsact_entt_rt {
	using entity_id_map_t = std::unordered_map
		< ::ecsact::entity_id
		, entt::entity
		>;

	template<typename Package>
	struct registry_info {
		using package = Package;

		std::optional<std::reference_wrapper<std::mutex>> mutex;
		::entt::registry registry;
		entity_id_map_t entities_map;

		/**
		 * Index of this vector is a statically casted EnTT ID
		 */
		std::vector<::ecsact::entity_id> _ecsact_entity_ids;

		::ecsact::entity_id last_entity_id{};

		struct create_new_entity_result {
			entt::entity entt_entity_id;
			::ecsact::entity_id ecsact_entity_id;
		};

		void init_registry() {
			using boost::mp11::mp_for_each;
			using ecsact::entt::detail::beforechange_storage;
			using ecsact::entt::detail::temp_storage;
			using ecsact::entt::component_added;
			using ecsact::entt::component_changed;
			using ecsact::entt::component_removed;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				registry.storage<C>();
			});

			mp_for_each<typename package::system_addables>([&]<typename C>(C) {
				registry.storage<component_added<C>>();
			});

			mp_for_each<typename package::system_writables>([&]<typename C>(C) {
				registry.storage<beforechange_storage<C>>();
				registry.storage<component_changed<C>>();
			});

			mp_for_each<typename package::system_removables>([&]<typename C>(C) {
				registry.storage<temp_storage<C>>();
				registry.storage<component_removed<C>>();
			});
		}

		template<typename C> requires(std::is_empty_v<C>)
		void add_component
			( ::entt::entity entity
			)
		{
			registry.emplace<C>(entity);
		}

		template<typename C, typename... Args> requires(!std::is_empty_v<C>)
		void add_component
			( ::entt::entity  entity
			, Args&&...       args
			)
		{
			using boost::mp11::mp_for_each;

			registry.emplace<C>(entity, std::forward<Args>(args)...);

			mp_for_each<typename package::system_writables>([&]<typename O>(O) {
				if constexpr(C::transient) return;
				if constexpr(std::is_same_v<std::remove_cvref_t<C>, O>) {
					using ecsact::entt::detail::beforechange_storage;
					beforechange_storage<O> beforechange = {
						.value{std::forward<Args>(args)...},
						.set = false,
					};
					registry.emplace<beforechange_storage<O>>(
						entity,
						std::move(beforechange)
					);
				}
			});
		}

		template<typename C>
		void remove_component
			( ::entt::entity  entity
			)
		{
			using boost::mp11::mp_for_each;

			registry.erase<C>(entity);

			mp_for_each<typename package::system_writables>([&]<typename O>(O) {
				if constexpr(C::transient) return;
				if constexpr(std::is_same_v<std::remove_cvref_t<C>, O>) {
					using ecsact::entt::detail::beforechange_storage;
					registry.erase<beforechange_storage<O>>(entity);
				}
			});
		}

		/** @internal */
		inline auto _create_entity
			( ::ecsact::entity_id ecsact_entity_id
			)
		{
			auto new_entt_entity_id = registry.create();
			entities_map[ecsact_entity_id] = new_entt_entity_id;
			_ecsact_entity_ids.resize(static_cast<size_t>(new_entt_entity_id) + 1);
			_ecsact_entity_ids[_ecsact_entity_ids.size() - 1] = ecsact_entity_id;
			return new_entt_entity_id;
		}

		/** @internal */
		inline create_new_entity_result _create_entity() {
			auto new_entity_id = static_cast<::ecsact::entity_id>(
				static_cast<int>(last_entity_id) + 1
			);
			while(entities_map.contains(new_entity_id)) {
				new_entity_id = static_cast<::ecsact::entity_id>(
					static_cast<int>(new_entity_id) + 1
				);
			}
			last_entity_id = new_entity_id;
			return {
				.entt_entity_id = _create_entity(new_entity_id),
				.ecsact_entity_id = new_entity_id,
			};
		}
	
		// Creates an entity and also makes sure there is a matching one in the
		// pending registry
		inline auto create_entity
			( ::ecsact::entity_id ecsact_entity_id
			)
		{
			std::scoped_lock lk(mutex->get());
			return _create_entity(ecsact_entity_id);
		}
		inline auto create_entity() {
			std::scoped_lock lk(mutex->get());
			return _create_entity();
		}

		entt::entity entt_entity_id
			( ::ecsact::entity_id ecsact_entity_id
			) const
		{
			return entities_map.at(ecsact_entity_id);
		}

		::ecsact::entity_id ecsact_entity_id
			( entt::entity entt_entity_id
			) const
		{
			return _ecsact_entity_ids.at(static_cast<size_t>(entt_entity_id));
		}
	};
}
