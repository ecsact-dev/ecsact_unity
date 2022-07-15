#pragma once

#include <stdexcept>
#include <type_traits>
#include <string>
#include <unordered_set>
#include <boost/mp11.hpp>
#include <entt/entt.hpp>

#include "registry_info.hh"
#include "event_markers.hh"
#include "system_entt_view.hh"

namespace ecsact_entt_rt {
	struct system_execution_context_base;
}

struct ecsact_system_execution_context {
	ecsact::system_id system_id;
	// System execution context implementation. To be casted to specific derived
	// templated type. See `system_execution_context<Package, System>`
	ecsact_entt_rt::system_execution_context_base* impl;
};

namespace ecsact_entt_rt {

	struct system_execution_context_base {
		using cptr_t = struct ::ecsact_system_execution_context*;
		using const_cptr_t = const struct ::ecsact_system_execution_context*;
		using cpp_ptr_t = ecsact::detail::system_execution_context*;
		using const_cpp_ptr_t = ecsact::detail::system_execution_context*;

		::entt::entity entity;
		const cptr_t parent;
		const void* action;
	};

	template<typename Package, typename SystemT>
	struct system_execution_context : system_execution_context_base {
		using system_execution_context_base::cptr_t;
		using system_execution_context_base::const_cptr_t;
		using system_execution_context_base::cpp_ptr_t;
		using system_execution_context_base::const_cpp_ptr_t;

		using package = Package;
		using view_type = ecsact::entt::system_view_type<SystemT>;
		ecsact_entt_rt::registry_info<Package>& info;
		view_type& view;

		/** @internal */
		ecsact_system_execution_context _c_ctx;

		system_execution_context
			( ecsact_entt_rt::registry_info<Package>&  info
			, view_type&                               view
			, ::entt::entity                           entity
			, const cptr_t                             parent
			, const void*                              action
			)
			: system_execution_context_base{entity, parent, action}
			, info(info)
			, view(view)
		{
			_c_ctx.system_id = static_cast<::ecsact::system_id>(SystemT::id);
			_c_ctx.impl = this;
		}

		/**
		 * Pointer for ecsact C system execution
		 */
		inline cptr_t cptr() noexcept {
			return reinterpret_cast<cptr_t>(&_c_ctx);
		}

		/**
		 * Pointer for ecsact C system execution
		 */
		inline const_cptr_t cptr() const noexcept {
			return reinterpret_cast<const_cptr_t>(&_c_ctx);
		}

		/**
		 * Pointer for ecsact C++ system execution
		 */
		inline cpp_ptr_t cpp_ptr() noexcept {
			return reinterpret_cast<cpp_ptr_t>(cptr());
		}

		/**
		 * Pointer for ecsact C++ system execution
		 */
		inline const_cpp_ptr_t cpp_ptr() const noexcept {
			return reinterpret_cast<const_cpp_ptr_t>(cptr());
		}

		template<typename C>
		void add
			( const C& component
			)
		{
			using ecsact::entt::component_added;
			using ecsact::entt::component_removed;
			using ecsact::entt::detail::pending_add;
			using namespace std::string_literals;

#ifndef NDEBUG
			{
				const bool already_has_component =
					info.registry.template all_of<pending_add<C>>(entity);
				if(already_has_component) {
					std::string err_msg = "Cannot call ctx.add() multiple times. ";
					err_msg += "Added component: "s + typeid(C).name();
					throw std::runtime_error(err_msg.c_str());
				}
			}
#endif

			if constexpr(std::is_empty_v<C>) {
				info.registry.template emplace<pending_add<C>>(entity);
			} else {
				info.registry.template emplace<pending_add<C>>(entity, component);
			}

			if constexpr(!C::transient) {
				if(info.registry.template all_of<component_removed<C>>(entity)) {
					info.registry.template remove<component_removed<C>>(entity);
				} else {
					info.registry.template emplace<component_added<C>>(entity);
				}
			}
		}

		void add
			( ::ecsact::component_id  component_id
			, const void*             component_data
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					add<C>(*static_cast<const C*>(component_data));
				}
			});
		}

		template<typename C>
		void remove() {
			using ecsact::entt::component_removed;
			using ecsact::entt::component_added;
			using ecsact::entt::detail::temp_storage;
			using ecsact::entt::detail::pending_remove;

#ifndef NDEBUG
			[[maybe_unused]] auto component_name = typeid(C).name();

			{
				const bool already_has_component =
					info.registry.template all_of<C>(entity);
				if(!already_has_component) {
					std::string err_msg = "Cannot call ctx.remove() multiple times. ";
					err_msg += "Removed component: ";
					err_msg += component_name;
					throw std::runtime_error(err_msg.c_str());
				}
			}
#endif

			if constexpr(!C::transient) {
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
			}

			info.registry.template emplace<pending_remove<C>>(entity);

			if constexpr(!C::transient) {
				info.registry.template emplace<component_removed<C>>(entity);
			}
		}

		void remove
			( ::ecsact::component_id  component_id
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					remove<C>();
				}
			});
		}

		template<typename C> requires(!std::is_empty_v<C>)
		const C& get() {
			return view.template get<C>(entity);
		}

		void get
			( ::ecsact::component_id  component_id
			, void*                   out_component_data
			)
		{
			using boost::mp11::mp_for_each;
			using boost::mp11::mp_unique;
			using boost::mp11::mp_push_back;
			using boost::mp11::mp_flatten;

			using gettable_components = mp_unique<mp_flatten<mp_push_back<
				typename SystemT::writables,
				typename SystemT::readables
			>>>;
			mp_for_each<gettable_components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					if constexpr(!std::is_empty_v<C>) {
						C& out_component = *reinterpret_cast<C*>(out_component_data);
						out_component = get<C>();
					}
				}
			});
		}

		template<typename C> requires(!std::is_empty_v<C>)
		void update(const C& c) {
			using boost::mp11::mp_apply;
			using boost::mp11::mp_bind_front;
			using boost::mp11::mp_transform_q;
			using boost::mp11::mp_any;

			using ecsact::entt::detail::beforechange_storage;
			using ecsact::entt::component_changed;

			constexpr bool is_writable = mp_apply<mp_any, mp_transform_q<
				mp_bind_front<std::is_same, std::remove_cvref_t<C>>,
				typename SystemT::writables
			>>::value;

			static_assert(is_writable);

			C& comp = view.template get<C>(entity);
			auto& beforechange = view.template get<beforechange_storage<C>>(entity);
			if(!beforechange.set) {
				beforechange.value = comp;
				beforechange.set = true;

				info.registry.template emplace_or_replace<component_changed<C>>(
					entity
				);
			}
			comp = c;
		}

		void update
			( ::ecsact::component_id  component_id
			, const void*             component_data
			)
		{
			using boost::mp11::mp_for_each;

			mp_for_each<typename SystemT::writables>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					update<C>(*reinterpret_cast<const C*>(component_data));
				}
			});
		}

		template<typename ComponentT>
		bool has() {
			return info.registry.template all_of<ComponentT>(entity);
		}

		bool has
			( ::ecsact::component_id  component_id
			)
		{
			using boost::mp11::mp_for_each;

			bool result = false;
			mp_for_each<typename package::components>([&]<typename C>(const C&) {
				if(C::id == component_id) {
					result = has<C>();
				}
			});
			return result;
		}

		void generate
			( int                      component_count
			, ::ecsact::component_id*  component_ids
			, const void**             components_data
			)
		{
			using boost::mp11::mp_for_each;
			using ecsact::entt::component_added;
			using ecsact::entt::detail::pending_add;

			auto new_entity = info.create_entity().entt_entity_id;
			for(int i=0; component_count > i; ++i) {
				auto component_id = component_ids[i];
				auto component_data = components_data[i];
				mp_for_each<typename package::components>([&]<typename C>(const C&) {
					if(C::id == component_id) {
						if constexpr(std::is_empty_v<C>) {
							info.registry.template emplace<pending_add<C>>(new_entity);
						} else {
							info.registry.template emplace<pending_add<C>>(
								new_entity,
								*static_cast<const C*>(component_data)
							);
						}

						info.registry.template emplace<component_added<C>>(new_entity);
					}
				});
			}
		}
	};
}