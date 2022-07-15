#pragma once

#include <ecsact/runtime.hh>
#include <ecsact/runtime/core.h>

#define ECSACT_CAST_FNS(IdTypeName)\
	constexpr ecsact_##IdTypeName cppid_to_cid\
		( ::ecsact::IdTypeName id\
		)\
	{\
		return static_cast<ecsact_##IdTypeName>(id);\
	}\
	constexpr ::ecsact::IdTypeName cid_to_cppid\
		( ecsact_##IdTypeName id\
		)\
	{\
		return static_cast<::ecsact::IdTypeName>(id);\
	}\
	inline ecsact_##IdTypeName* cppid_to_cid\
		( ::ecsact::IdTypeName* id\
		)\
	{\
		return reinterpret_cast<ecsact_##IdTypeName*>(id);\
	}\
	inline ::ecsact::IdTypeName* cid_to_cppid\
		( ecsact_##IdTypeName* id\
		)\
	{\
		return reinterpret_cast<::ecsact::IdTypeName*>(id);\
	}\
	inline const ecsact_##IdTypeName* cppid_to_cid\
		( const ::ecsact::IdTypeName* id\
		)\
	{\
		return reinterpret_cast<const ecsact_##IdTypeName*>(id);\
	}\
	inline const ::ecsact::IdTypeName* cid_to_cppid\
		( const ecsact_##IdTypeName* id\
		)\
	{\
		return reinterpret_cast<const ::ecsact::IdTypeName*>(id);\
	}

namespace ecsact {
	ECSACT_CAST_FNS(system_id)
	ECSACT_CAST_FNS(component_id)
	ECSACT_CAST_FNS(variant_id)
	ECSACT_CAST_FNS(registry_id)
	ECSACT_CAST_FNS(entity_id)
}

#undef ECSACT_CAST_FNS
