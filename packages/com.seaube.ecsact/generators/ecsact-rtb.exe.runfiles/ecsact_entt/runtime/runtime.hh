#pragma once

#include <cassert>
#include <vector>
#include <tuple>
#include <functional>
#include <unordered_map>
#include <optional>
#include <mutex>
#include <tuple>
#include <execution>
#include <span>
#include <boost/mp11.hpp>
#include <ecsact/runtime.hh>
#include <ecsact/runtime/common.h>
#include <ecsact/runtime/core.h>
#include <ecsact/lib.hh>
#include <entt/entt.hpp>

#include "system_execution_context.hh"
#include "execution_events_collector.hh"
#include "registry_info.hh"
#include "event_markers.hh"
#include "system_entt_view.hh"

namespace ecsact::entt {
	template<::ecsact::package Package>
	class runtime {
		/**
		 * Checks if type T is listd as one of the actions in the ecact package.
		 * @returns `true` if T is a component belonging to `package`, `false` 
		 *          otherwise.
		 */
		template<typename T>
		static constexpr bool is_action() {
			using boost::mp11::mp_bind_front;
			using boost::mp11::mp_transform_q;
			using boost::mp11::mp_any;
			using boost::mp11::mp_apply;

			return mp_apply<mp_any, mp_transform_q<
				mp_bind_front<std::is_same, std::remove_cvref_t<T>>,
				typename Package::actions
			>>::value;
		}

		using registry_info = ecsact_entt_rt::registry_info<Package>;

		using registries_map_t = std::unordered_map
			< ::ecsact::registry_id
			, registry_info
			>;

		using actions_span_t = std::span<ecsact_action, std::dynamic_extent>;

		::ecsact::registry_id _last_registry_id{};
		registries_map_t _registries;

#ifdef ECSACT_ENTT_RUNTIME_DYNAMIC_SYSTEM_IMPLS
		using sys_impl_fns_t =
			std::unordered_map<ecsact_system_id, ecsact_system_execution_impl>;
		sys_impl_fns_t _sys_impl_fns;
#endif

	public:
		template<typename SystemT>
		using system_execution_context =
			ecsact_entt_rt::system_execution_context<Package, SystemT>;
		using execution_events_collector =
			ecsact_entt_rt::execution_events_collector;
		using registry_type = ::entt::registry;
		using entt_entity_type = typename registry_type::entity_type;
		using package = Package;

		::ecsact::registry_id create_registry
			( const char* registry_name
			)
		{
			using boost::mp11::mp_for_each;

			// Using the index of _registries as an ID
			const auto reg_id = static_cast<::ecsact::registry_id>(
				static_cast<int>(_last_registry_id) + 1
			);

			_last_registry_id = reg_id;

			auto itr = _registries.emplace_hint(
				_registries.end(),
				std::piecewise_construct,
				std::forward_as_tuple(reg_id),
				std::forward_as_tuple()
			);

			registry_info& info = itr->second;
			info.init_registry();
			return reg_id;
		}

		void destroy_registry
			( ::ecsact::registry_id reg_id
			)
		{
			_registries.erase(reg_id);
		}

		void clear_registry
			( ::ecsact::registry_id reg_id
			)
		{
			using boost::mp11::mp_for_each;

			auto& info = _registries.at(reg_id);

			info.registry = {};
			info.init_registry();
			info.entities_map.clear();
			info._ecsact_entity_ids.clear();
			info.last_entity_id = {};
		}

		::ecsact::entity_id create_entity
			( ::ecsact::registry_id reg_id
			)
		{
			std::mutex mutex;
			auto& info = _registries.at(reg_id);
			info.mutex = mutex;
			auto new_entity_id = info.create_entity().ecsact_entity_id;
			info.mutex = std::nullopt;
			return new_entity_id;
		}

		void ensure_entity
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			auto& info = _registries.at(reg_id);
			if(!info.entities_map.contains(entity_id)) {
				std::mutex mutex;
				info.mutex = mutex;
				info.create_entity(entity_id);
				info.mutex = std::nullopt;
			}
		}

		bool entity_exists
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			auto& info = _registries.at(reg_id);
			return info.entities_map.contains(entity_id);
		}

		void destroy_entity
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			using boost::mp11::mp_for_each;

			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);

			info.registry.destroy(entt_entity_id);
			info.entities_map.erase(entity_id);
		}

		int count_entities
			( ::ecsact::registry_id reg_id
			)
		{
			auto& info = _registries.at(reg_id);
			return static_cast<int>(info.registry.size());
		}

		std::vector<ecsact::entity_id> get_entities
			( ::ecsact::registry_id reg_id
			)
		{
			auto& info = _registries.at(reg_id);
			std::vector<ecsact::entity_id> result;
			for(auto& entry: info.entities_map) {
				result.push_back(entry.first);
			}

			return result;
		}

		void get_entities
			( ::ecsact::registry_id  reg_id
			, int                    max_entities_count
			, ::ecsact::entity_id*   out_entities
			, int*                   out_entities_count
			)
		{
			auto& info = _registries.at(reg_id);

			int entities_count = static_cast<int>(info.entities_map.size());
			max_entities_count = std::min(entities_count, max_entities_count);

			auto itr = info.entities_map.begin();
			for(int i=0; max_entities_count > i; ++i) {
				if(itr == info.entities_map.end()) break;

				out_entities[i] = itr->first;
				++itr;
			}

			if(out_entities_count != nullptr) {
				*out_entities_count = entities_count;
			}
		}

		template<typename C>
		void add_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			, const C&               component_data
			)
		{
			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);

			if constexpr(std::is_empty_v<C>) {
				info.template add_component<C>(entt_entity_id);
			} else {
				info.template add_component<C>(entt_entity_id, component_data);
			}
		}

		template<typename ComponentT>
		void add_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			add_component<ComponentT>(reg_id, entity_id, ComponentT{});
		}

		void add_component
			( ::ecsact::registry_id   reg_id
			, ::ecsact::entity_id     entity_id
			, ::ecsact::component_id  component_id
			, const void*             component_data
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					if constexpr(std::is_empty_v<C>) {
						add_component<C>(reg_id, entity_id);
					} else {
						add_component<C>(
							reg_id,
							entity_id,
							*static_cast<const C*>(component_data)
						);
					}
				}
			});
		}

		template<typename ComponentT>
		bool has_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);

			return info.registry.template all_of<ComponentT>(entt_entity_id);
		}

		bool has_component
			( ::ecsact::registry_id   reg_id
			, ::ecsact::entity_id     entity_id
			, ::ecsact::component_id  component_id
			)
		{
			using boost::mp11::mp_for_each;

			bool result = false;
			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					result = has_component<C>(reg_id, entity_id);
				}
			});
			return result;
		}

		template<typename ComponentT>
		const ComponentT& get_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);

			return info.registry.template get<ComponentT>(entt_entity_id);
		}

		const void* get_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			, ::ecsact::component_id  component_id
			)
		{
			using boost::mp11::mp_for_each;

			const void* component_data = nullptr;
			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					if constexpr(std::is_empty_v<C>) {
						static C c{};
						component_data = &c;
					} else {
						const C& comp_ref = get_component<C>(reg_id, entity_id);
						component_data = &comp_ref;
					}
				}
			});
			return component_data;
		}

		int count_components
			( ecsact::registry_id  registry_id
			, ecsact::entity_id    entity_id
			)
		{
			using boost::mp11::mp_for_each;

			int count = 0;
			mp_for_each<typename package::components>([&]<typename C>(C) {
				if(has_component<C>(registry_id, entity_id)) {
					count += 1;
				}
			});
			return count;
		}

		void each_component
			( ecsact::registry_id             registry_id
			, ecsact::entity_id               entity_id
			, ecsact_each_component_callback  callback
			, void*                           callback_user_data
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if(has_component<C>(registry_id, entity_id)) {
					if constexpr(std::is_empty_v<C>) {
						callback(
							static_cast<ecsact_component_id>(C::id),
							nullptr,
							callback_user_data
						);
					} else {
						callback(
							static_cast<ecsact_component_id>(C::id),
							&get_component<C>(registry_id, entity_id),
							callback_user_data
						);
					}
				}
			});
		}

		void get_components
			( ecsact::registry_id    registry_id
			, ecsact::entity_id      entity_id
			, int                    max_components_count
			, ecsact::component_id*  out_component_ids
			, const void**           out_components_data
			, int*                   out_components_count
			)
		{
			using boost::mp11::mp_for_each;

			int index = 0;
			mp_for_each<typename package::components>([&]<typename C>(C) {
				if(index >= max_components_count) return;

				if(has_component<C>(registry_id, entity_id)) {
					index += 1;
					out_component_ids[index] = C::id;
					if constexpr(std::is_empty_v<C>) {
						out_components_data[index] = nullptr;
					} else {
						out_components_data[index] = &get_component<C>(
							registry_id,
							entity_id
						);
					}
				}
			});

			if(out_components_count != nullptr) {
				*out_components_count = index;
			}
		}

		template<typename ComponentT>
		void update_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			, const ComponentT&      component_data
			)
		{
			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);
	
			auto& component = info.registry.template get<ComponentT>(entt_entity_id);
			component = component_data;
		}

		void update_component
			( ::ecsact::registry_id   reg_id
			, ::ecsact::entity_id     entity_id
			, ::ecsact::component_id  component_id
			, const void*             component_data
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					if constexpr(!std::is_empty_v<C>) {
						update_component<C>(
							reg_id,
							entity_id,
							*static_cast<const C*>(component_data)
						);
					}
				}
			});
		}

		template<typename C>
		void remove_component
			( ::ecsact::registry_id  reg_id
			, ::ecsact::entity_id    entity_id
			)
		{
			auto& info = _registries.at(reg_id);
			auto entt_entity_id = info.entities_map.at(entity_id);

			info.template remove_component<C>(entt_entity_id);
		}

		void remove_component
			( ::ecsact::registry_id   reg_id
			, ::ecsact::entity_id     entity_id
			, ::ecsact::component_id  component_id
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if(C::id == component_id) {
					remove_component<C>(reg_id, entity_id);
				}
			});
		}

		size_t component_size
			( ::ecsact::component_id comp_id
			)
		{
			using boost::mp11::mp_for_each;

			size_t comp_size = 0;
			mp_for_each<typename package::components>([&]<typename C>(C) {
				if(C::id == comp_id) {
					comp_size = sizeof(C);
				}
			});
			return comp_size;
		}

		size_t action_size
			( ::ecsact::action_id action_id
			)
		{
			using boost::mp11::mp_for_each;

			size_t act_size = 0;
			mp_for_each<typename package::actions>([&]<typename A>(A) {
				if(A::id == action_id) {
					act_size = sizeof(A);
				}
			});
			return act_size;
		}

	private:
		template<typename SystemT>
		void _apply_pending_adds
			( registry_info& info
			)
		{
			using boost::mp11::mp_for_each;
			using boost::mp11::mp_unique;
			using boost::mp11::mp_flatten;
			using boost::mp11::mp_push_back;
			using ecsact::entt::detail::pending_add;

			// using flattened_generates = typename SystmT::generates

			using addables = mp_unique<mp_flatten<mp_push_back<
				typename SystemT::generates,
				typename SystemT::adds
			>>>;

			mp_for_each<addables>([&]<typename C>(C) {
				using boost::mp11::mp_apply;
				using boost::mp11::mp_bind_front;
				using boost::mp11::mp_transform_q;
				using boost::mp11::mp_any;

				// Making sure all the components in `addables` list are indeed package
				// components. If this assertion fails there was an error in creating
				// the `addables` alias.
				static_assert(
					mp_apply<mp_any, mp_transform_q<
						mp_bind_front<std::is_same, std::remove_cvref_t<C>>,
						typename Package::components
					>>::value
				);

				auto view = info.registry.template view<pending_add<C>>();
				if constexpr(std::is_empty_v<C>) {
					view.each([&](auto entity) {
						info.template add_component<C>(entity);
					});
				} else {
					view.each([&](auto entity, auto& component) {
						info.template add_component<C>(entity, component.value);
					});
				}

				info.registry.template clear<pending_add<C>>();
			});
		}

		template<typename SystemT>
		void _apply_pending_removes
			( registry_info& info
			)
		{
			using boost::mp11::mp_for_each;
			using ecsact::entt::detail::pending_remove;

			mp_for_each<typename SystemT::removes>([&]<typename C>(C) {
				auto view = info.registry.template view<pending_remove<C>>();
				view.each([&](auto entity) {
					info.template remove_component<C>(entity);
				});

				info.registry.template clear<pending_remove<C>>();
			});
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_trivial_removes_only
			( registry_info&                    info
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename SystemT::removes>([&]<typename C>(C) {
				info.registry.template clear<C>();
			});
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_trivial_default_itr
			( registry_info&                    info
			, system_view_type<SystemT>&        view
			, entt_entity_type                  entity
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_for_each;

			system_execution_context<SystemT> ctx(info, view, entity, parent, action);

			mp_for_each<typename SystemT::removes>([&]<typename C>(C) {
				ctx.template remove<C>();
			});
			mp_for_each<typename SystemT::adds>([&]<typename C>(C) {
				ctx.template add<C>(C{});
			});

			mp_for_each<ChildSystemsListT>([&]<typename SystemPair>(SystemPair) {
				using boost::mp11::mp_first;
				using boost::mp11::mp_second;
				using ChildSystemT = mp_first<SystemPair>;
				using GrandChildSystemsListT = mp_second<SystemPair>;

				_execute_system<ChildSystemT, GrandChildSystemsListT>(
					info,
					ctx.cptr(),
					actions
				);
			});
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_trivial_default
			( registry_info&                    info
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_empty;
			using std::execution::par_unseq;
			using std::execution::seq;

#ifndef NDEBUG
			[[maybe_unused]] auto system_name = typeid(SystemT).name();
#endif

			auto view = system_view<SystemT>(info.registry);

			constexpr bool can_exec_parallel =
				mp_empty<ChildSystemsListT>::value &&
				mp_empty<typename SystemT::adds>::value &&
				mp_empty<typename SystemT::removes>::value &&
				mp_empty<typename SystemT::generates>::value;

			if constexpr(can_exec_parallel) {
				// TODO(zaucy): Make this par_unseq
				std::for_each(par_unseq, view.begin(), view.end(), [&](auto entity) {
					_execute_system_trivial_default_itr<SystemT, ChildSystemsListT>(
						info,
						view,
						entity,
						parent,
						action,
						actions
					);
				});
			} else {
				std::for_each(seq, view.begin(), view.end(), [&](auto entity) {
					_execute_system_trivial_default_itr<SystemT, ChildSystemsListT>(
						info,
						view,
						entity,
						parent,
						action,
						actions
					);
				});
			}
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_trivial
			( registry_info&                    info
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_for_each;
			using boost::mp11::mp_empty;
			using boost::mp11::mp_size;

			static_assert(SystemT::has_trivial_impl);

			using excludes_list = typename SystemT::excludes;
			using includes_list = typename SystemT::includes;
			using removes_list = typename SystemT::removes;
			using adds_list = typename SystemT::adds;

			// Check if we are doing a blanket remove for an optimized system
			// implementation.
			constexpr bool is_removes_only =
				mp_empty<excludes_list>::value && 
				mp_empty<adds_list>::value &&
				mp_empty<includes_list>::value &&
				(mp_size<removes_list>::value == 1);

			if constexpr(is_removes_only) {
				_execute_system_trivial_removes_only<SystemT, ChildSystemsListT>(
					info,
					parent,
					action,
					actions
				);
			} else {
				_execute_system_trivial_default<SystemT, ChildSystemsListT>(
					info,
					parent,
					action,
					actions
				);
			}
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_user_itr
			( registry_info&                    info
			, system_view_type<SystemT>&        view
			, entt_entity_type                  entity
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_for_each;

			[[maybe_unused]]
			const auto system_name = typeid(SystemT).name();
			const auto system_id = static_cast<ecsact_system_id>(SystemT::id);

			system_execution_context<SystemT> ctx(info, view, entity, parent, action);

			// Execute the user defined system implementation
#ifdef ECSACT_ENTT_RUNTIME_DYNAMIC_SYSTEM_IMPLS
			if(_sys_impl_fns.contains(system_id)) {
				_sys_impl_fns.at(system_id)(ctx.cptr());
			}
#	ifdef ECSACT_ENTT_RUNTIME_STATIC_SYSTEM_IMPLS
			else
#	endif
#endif

#ifdef ECSACT_ENTT_RUNTIME_STATIC_SYSTEM_IMPLS
			{
				SystemT::invoke_static_impl(ctx.cptr());
			}
#endif

			mp_for_each<ChildSystemsListT>([&]<typename SystemPair>(SystemPair) {
				using boost::mp11::mp_first;
				using boost::mp11::mp_second;
				using ChildSystemT = mp_first<SystemPair>;
				using GrandChildSystemsListT = mp_second<SystemPair>;

				_execute_system<ChildSystemT, GrandChildSystemsListT>(
					info,
					ctx.cptr(),
					actions
				);
			});
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system_user
			( registry_info&                    info
			, ecsact_system_execution_context*  parent
			, const void*                       action
			, const actions_span_t&             actions
			)
		{
			using boost::mp11::mp_empty;
			using std::execution::seq;
			using std::execution::par_unseq;

			static_assert(!SystemT::has_trivial_impl);

			auto view = system_view<SystemT>(info.registry);

			constexpr bool can_exec_parallel =
				mp_empty<ChildSystemsListT>::value &&
				mp_empty<typename SystemT::adds>::value &&
				mp_empty<typename SystemT::removes>::value;

			if constexpr(can_exec_parallel) {
				// TODO(zaucy): Make this par_unseq
				std::for_each(seq, view.begin(), view.end(), [&](auto entity) {
					_execute_system_user_itr<SystemT, ChildSystemsListT>(
						info,
						view,
						entity,
						parent,
						action,
						actions
					);
				});
			} else {
				std::for_each(seq, view.begin(), view.end(), [&](auto entity) {
					_execute_system_user_itr<SystemT, ChildSystemsListT>(
						info,
						view,
						entity,
						parent,
						action,
						actions
					);
				});
			}
		}

		template<typename SystemT, typename ChildSystemsListT>
		void _execute_system
			( registry_info&                    info
			, ecsact_system_execution_context*  parent
			, const actions_span_t&             actions
			)
		{
			if constexpr(is_action<SystemT>()) {
				for(const ecsact_action& action : actions) {
					if(action.action_id == static_cast<ecsact_system_id>(SystemT::id)) {
						if constexpr(SystemT::has_trivial_impl) {
							_execute_system_trivial<SystemT, ChildSystemsListT>(
								info,
								parent,
								static_cast<const SystemT*>(action.action_data),
								actions
							);
						} else {
							_execute_system_user<SystemT, ChildSystemsListT>(
								info,
								parent,
								static_cast<const SystemT*>(action.action_data),
								actions
							);
						}
					}
				}
			} else {
				if constexpr(SystemT::has_trivial_impl) {
					_execute_system_trivial<SystemT, ChildSystemsListT>(
						info,
						parent,
						nullptr,
						actions
					);
				} else {
					_execute_system_user<SystemT, ChildSystemsListT>(
						info,
						parent,
						nullptr,
						actions
					);
				}
			}

			_apply_pending_removes<SystemT>(info);
			_apply_pending_adds<SystemT>(info);
		}

		void _clear_transients
			( registry_info& info
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				// Transients require no processing, just clear.
				if constexpr(C::transient) {
					info.registry.template clear<C>();
				}
			});
		}

		void _sort_components
			( registry_info& info
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(!std::is_empty_v<C> && !C::transient) {
					// Sorting for deterministic order of components when executing
					// systems.
					// TODO(zaucy): This sort is only necessary for components part of a 
					//              system execution hierarchy greater than 1.
					info.registry.template sort<C>([](const C& a, const C& b) -> bool {
						return a < b;
					});
				}
			});
		}

		void _trigger_init_component_events
			( registry_info&               info
			, execution_events_collector&  events_collector
			)
		{
			using boost::mp11::mp_for_each;

			if(!events_collector.has_init_callback()) return;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(C::transient) return;

				::entt::basic_view added_view{
					info.registry.template storage<C>(),
					info.registry.template storage<component_added<C>>(),
				};
				
				for(entt_entity_type entity : added_view) {
					if constexpr(std::is_empty_v<C>) {
						events_collector.invoke_init_callback<C>(
							info.ecsact_entity_id(entity)
						);
					} else {
						events_collector.invoke_init_callback<C>(
							info.ecsact_entity_id(entity),
							added_view.template get<C>(entity)
						);
					}
				}
			});
		}

		void _trigger_update_component_events
			( registry_info&               info
			, execution_events_collector&  events_collector
			)
		{
			using boost::mp11::mp_for_each;
			using detail::beforechange_storage;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(!C::transient && !std::is_empty_v<C>) {
					::entt::basic_view changed_view{
						info.registry.template storage<C>(),
						info.registry.template storage<beforechange_storage<C>>(),
						info.registry.template storage<component_changed<C>>(),
					};
					
					for(entt_entity_type entity : changed_view) {
						auto& before = changed_view.template get<beforechange_storage<C>>(
							entity
						);
						auto& current = changed_view.template get<C>(entity);
						
						if(before.value != current) {
							events_collector.invoke_update_callback<C>(
								info.ecsact_entity_id(entity),
								current
							);
						}
						before.set = false;
					}
				}
			});
		}

		void _trigger_remove_component_events
			( registry_info&               info
			, execution_events_collector&  events_collector
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(C::transient) return;

				::entt::basic_view removed_view{
					info.registry.template storage<detail::temp_storage<C>>(),
					info.registry.template storage<component_removed<C>>(),
				};

				for(entt_entity_type entity : removed_view) {
					if constexpr(std::is_empty_v<C>) {
						events_collector.invoke_remove_callback<C>(
							info.ecsact_entity_id(entity)
						);
					} else {
						events_collector.invoke_remove_callback<C>(
							info.ecsact_entity_id(entity),
							removed_view.template get<detail::temp_storage<C>>(entity).value
						);
					}

					info.registry.template storage<detail::temp_storage<C>>().remove(
						entity
					);
				}
			});
		}

		void _execute_systems
			( registry_info&   info
			, actions_span_t&  actions
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::execution_order>(
				[&]<typename SystemList>(SystemList) {
					using boost::mp11::mp_size;
					using boost::mp11::mp_empty;
					using boost::mp11::mp_first;
					using boost::mp11::mp_second;
					using std::execution::par_unseq;

					if constexpr(mp_size<SystemList>::value > 1) {
						mp_for_each<SystemList>([&]<typename SystemPair>(SystemPair) {
							using SystemT = mp_first<SystemPair>;
							using ChildSystemsListT = mp_second<SystemPair>;
							_execute_system<SystemT, ChildSystemsListT>(
								info,
								nullptr,
								actions
							);
						});
					} else if constexpr(!mp_empty<SystemList>::value) {
						using SystemPair = mp_first<SystemList>;
						using SystemT = mp_first<SystemPair>;
						using ChildSystemsListT = mp_second<SystemPair>;
						_execute_system<SystemT, ChildSystemsListT>(
							info,
							nullptr,
							actions
						);
					}
				}
			);
		}

		template<typename C>
			requires(!std::is_empty_v<C>)
		void _pre_exec_add_component
			( registry_info&    info
			, entt_entity_type  entity
			, const C&          component
			)
		{
#ifndef NDEBUG
			{
				const bool already_has_component =
					info.registry.template all_of<C>(entity);
				if(already_has_component) {
					using namespace std::string_literals;
					std::string err_msg = "Entity already has component. ";
					err_msg += "Attempted added component: "s + typeid(C).name();
					throw std::runtime_error(err_msg.c_str());
				}
			}
#endif

			info.template add_component<C>(entity, component);
			info.registry.template emplace<component_added<C>>(entity);
		}

		template<typename C>
			requires(std::is_empty_v<C>)
		void _pre_exec_add_component
			( registry_info&    info
			, entt_entity_type  entity
			)
		{
#ifndef NDEBUG
			if(info.registry.template all_of<C>(entity)) {
				using namespace std::string_literals;
				std::string err_msg = "Entity already has component. ";
				err_msg += "Attempted added component: "s + typeid(C).name();
				throw std::runtime_error(err_msg.c_str());
			}
#endif

			info.template add_component<C>(entity);
			info.registry.template emplace<component_added<C>>(entity);
		}

		template<typename C>
			requires(!std::is_empty_v<C>)
		void _pre_exec_update_component
			( registry_info&    info
			, entt_entity_type  entity
			, const C&          updated_component
			)
		{
			using detail::beforechange_storage;

#ifndef NDEBUG
			if(!info.registry.template all_of<C>(entity)) {
				using namespace std::string_literals;
				std::string err_msg = "Entity does not have component. ";
				err_msg += "Attempted update on component: "s + typeid(C).name();
				throw std::runtime_error(err_msg.c_str());
			}
#endif

			C& component = info.registry.template get<C>(entity);
			if(info.registry.template all_of<beforechange_storage<C>>(entity)) {
				auto& before =
					info.registry.template get<beforechange_storage<C>>(entity);
				if(!before.set) {
					before.value = component;
					before.set = true;
				}
			} else {
				info.registry.template emplace<beforechange_storage<C>>(
					entity,
					component,
					true
				);
			}

			component = updated_component;

			if(!info.registry.template all_of<component_added<C>>(entity)) {
				info.registry.template emplace_or_replace<component_changed<C>>(entity);
			}
		}

		template<typename C>
		void _pre_exec_remove_component
			( registry_info&    info
			, entt_entity_type  entity
			)
		{
			using detail::temp_storage;

			if(info.registry.template all_of<component_added<C>>(entity)) {
				info.registry.template remove<component_added<C>>(entity);
			}
			if constexpr(!std::is_empty_v<C>) {
				auto& temp = info.registry.template storage<temp_storage<C>>();

				// Store current value of component for the before_remove event later
				if(temp.contains(entity)) {
					temp.get(entity).value = info.registry.template get<C>(entity);
				} else {
					temp.emplace(entity, info.registry.template get<C>(entity));
				}
			}

			info.template remove_component<C>(entity);
			if(!info.registry.template all_of<component_removed<C>>(entity)) {
				info.registry.template emplace<component_removed<C>>(entity);
			}
			info.registry.template remove<component_changed<C>>(entity);
			info.registry.template remove<component_added<C>>(entity);
		}

		void _apply_execution_options
			( const ecsact_execution_options&  options
			, registry_info&                   info
			)
		{
			using boost::mp11::mp_for_each;

			for(int i=0; options.add_components_length > i; ++i) {
				const ecsact_entity_id& entity = options.add_components_entities[i];
				const ecsact_component& comp = options.add_components[i];

				mp_for_each<typename package::components>([&]<typename C>(C) {
					if constexpr(C::transient) return;

					if(comp.component_id == static_cast<ecsact_component_id>(C::id)) {
						if constexpr(std::is_empty_v<C>) {
							_pre_exec_add_component<C>(
								info,
								info.entities_map.at(static_cast<::ecsact::entity_id>(entity))
							);
						} else {
							_pre_exec_add_component<C>(
								info,
								info.entities_map.at(static_cast<::ecsact::entity_id>(entity)),
								*static_cast<const C*>(comp.component_data)
							);
						}
					}
				});
			}

			for(int i=0; options.update_components_length > i; ++i) {
				const ecsact_entity_id& entity = options.update_components_entities[i];
				const ecsact_component& comp = options.update_components[i];

				mp_for_each<typename package::components>([&]<typename C>(C) {
					if constexpr(C::transient) return;

					if(comp.component_id == static_cast<ecsact_component_id>(C::id)) {
						if constexpr(!std::is_empty_v<C>) {
							_pre_exec_update_component<C>(
								info,
								info.entities_map.at(static_cast<::ecsact::entity_id>(entity)),
								*static_cast<const C*>(comp.component_data)
							);
						} else {
							assert(!std::is_empty_v<C>);
						}
					}
				});
			}

			for(int i=0; options.remove_components_length > i; ++i) {
				const ecsact_entity_id& entity = options.update_components_entities[i];
				ecsact_component_id component_id = options.remove_components[i];

				mp_for_each<typename package::components>([&]<typename C>(C) {
					if constexpr(C::transient) return;

					if(component_id == static_cast<ecsact_component_id>(C::id)) {
						_pre_exec_remove_component<C>(
							info,
							info.entities_map.at(static_cast<::ecsact::entity_id>(entity))
						);
					}
				});
			}
		}

		void _clear_event_markers
			( registry_info& info
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(C::transient) return;

				info.registry.template clear<component_added<C>>();
			});

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(C::transient) return;

				info.registry.template storage<component_changed<C>>().clear();
			});

			mp_for_each<typename package::components>([&]<typename C>(C) {
				if constexpr(C::transient) return;

				info.registry.template clear<component_removed<C>>();
			});
		}

	public:
#ifdef ECSACT_ENTT_RUNTIME_DYNAMIC_SYSTEM_IMPLS
		bool set_system_execution_impl
			( ::ecsact::system_id           system_id
			, ecsact_system_execution_impl  exec_impl
			)
		{
			if(exec_impl == nullptr) {
				_sys_impl_fns.erase(static_cast<ecsact_system_id>(system_id));
			} else {
				_sys_impl_fns[static_cast<ecsact_system_id>(system_id)] = exec_impl;
			}
			return true;
		}
#endif

		void execute_systems
			( ::ecsact::registry_id                      reg_id
			, int                                        execution_count
			, const ecsact_execution_options*            execution_options_list
			, std::optional<execution_events_collector>  events_collector
			)
		{
			std::mutex mutex;
			auto& info = _registries.at(reg_id);
			info.mutex = std::ref(mutex);

			for(int n=0; execution_count > n; ++n) {
				actions_span_t actions;
				if(execution_options_list != nullptr) {
					_apply_execution_options(execution_options_list[n], info);
					if(execution_options_list->actions_length > 0) {
						actions = std::span(
							execution_options_list->actions,
							execution_options_list->actions_length
						);
					}
				}
				// _sort_components(info);
				_execute_systems(info, actions);
				_clear_transients(info);
			}
			if(events_collector) {
				_trigger_init_component_events(info, *events_collector);
				_trigger_update_component_events(info, *events_collector);
				_trigger_remove_component_events(info, *events_collector);
			}
			_clear_event_markers(info);

			info.mutex = std::nullopt;
		}

	};

}
