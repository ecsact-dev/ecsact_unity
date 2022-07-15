#pragma once

#include <vector>
#include <variant>
#include <cstdint>
#include <string>
#include <type_traits>
#include <concepts>
#include <array>
#include <tuple>

#define DEF_ecsact_TAG(tag_name)\
	struct tag_name##_tag_type {\
		constexpr bool operator==\
			( const tag_name ## _tag_type&\
			) const noexcept = default;\
	};\
	inline constexpr tag_name##_tag_type tag_name##_tag = {};\
	\
	template<typename T>\
	concept tag_name = std::remove_reference_t<T>::ecsact_tag == tag_name##_tag

namespace ecsact {
	using byte_t = std::uint8_t;
	using buffer_t = std::vector<byte_t>;
	using const_buffer_t = const buffer_t&;
	using mutable_buffer_t = buffer_t&;

	DEF_ecsact_TAG(entity_registry);
	DEF_ecsact_TAG(systems);
	DEF_ecsact_TAG(system);
	DEF_ecsact_TAG(package);
	DEF_ecsact_TAG(component);
	DEF_ecsact_TAG(variant);
	DEF_ecsact_TAG(named_component_list);
	DEF_ecsact_TAG(named_action_list);
	
	/**
	 * Helper type to contain an arbitrary amounts of types. Meant for 
	 * metaprogramming use.
	 * SEE: boost.mp11
	 */
	template<typename... T> struct mp_list {};

	/**
	 * 
	 */
	enum class system_capability_access {
		unset = 0,

		// Individual options
		readonly   =  0b01000000,
		writeonly  =  0b00100000,

		// Combinations of options
		readwrite      =  0b01100000,
		opt_readonly   =  0b11000000,
		opt_writeonly  =  0b10100000,
		opt_readwrite  =  0b11100000,
	};

	enum class system_capability_filter {
		include,
		exclude,
	};

	/**
	 * 'assignment' system capability options
	 */
	enum class system_capability_assignment {
		adds,
		removes,
	};

	using system_capability = std::variant
		< system_capability_access
		, system_capability_filter
		, system_capability_assignment
		>;

	/**
	 * 
	 */
	template<typename Key, typename SystemCapability = ::ecsact::system_capability>
	struct system_capability_item {
		using key_type = Key;
		SystemCapability value;
	};

	template<typename... T>
	using system_capability_list = std::tuple<system_capability_item<T>...>;

	struct common_meta_info {
		std::string name;
		std::string absolute_name;
		std::string relative_name;
	};

	template<typename... T>
	struct system_meta_info : common_meta_info {
		system_capability_list<T...> capabilities;
		bool is_action;
	};

	template<typename... Item>
	constexpr decltype(auto) make_system_capabilities
		( Item&&... item
		)
	{
		return std::make_tuple(
			::ecsact::system_capability_item
				< typename Item::key_type
				, ::ecsact::system_capability
				>(
				::ecsact::system_capability{item.value}
			)...
		);
	}

	/**
	 * Return the implicit filter. If the capability is filter only then the
	 * filter capability is returned.
	 */
	constexpr system_capability_filter get_implicit_system_capability_filter
		( system_capability cap
		)
	{
		return std::visit([](auto& cap) -> system_capability_filter {
			using cap_t = std::remove_cvref_t<decltype(cap)>;
			if constexpr(std::is_same_v<cap_t, system_capability_assignment>) {
				switch(cap) {
					case system_capability_assignment::adds:
						return system_capability_filter::exclude;
					case system_capability_assignment::removes:
						return system_capability_filter::include;
				}
			} else if constexpr(std::is_same_v<cap_t, system_capability_access>) {
				return system_capability_filter::include;
			} else {
				return cap;
			}
		}, cap);
	}

	template<typename T>
	concept lerpable =
		(std::is_integral_v<T> || std::is_floating_point_v<T>) &&
    !std::is_same_v<T, bool>;

	template<lerpable Lerpable>
	inline Lerpable lerp
		( Lerpable                 a
		, Lerpable                 b
		, std::floating_point auto t
		)
	{
		return Lerpable{
			static_cast<decltype(t)>(a) + t *
			(static_cast<decltype(t)>(b) - static_cast<decltype(t)>(a))
		};
	}

	inline bool lerp
		( bool                      a
		, bool                      b
		, std::floating_point auto  t
		)
	{
		if(t >= decltype(t){0.5}) {
			return b;
		} else {
			return a;
		}
	}

	template<typename T, std::size_t N>
	inline std::array<T, N> lerp
		( const std::array<T, N>&   a
		, const std::array<T, N>&   b
		, std::floating_point auto  t
		)
	{
		std::array<T, N> result;

		for(std::size_t i = 0; N > i; ++i) {
			result[i] = ::ecsact::lerp(a[i], b[i], t);
		}

		return result;
	}
}

#undef DEF_ecsact_TAG
