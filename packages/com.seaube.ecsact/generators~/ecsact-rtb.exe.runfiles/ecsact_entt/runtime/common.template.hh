#pragma once

#ifndef ECSACT_ENTT_RUNTIME_USER_HEADER
#	error ECSACT_ENTT_RUNTIME_USER_HEADER must be defined
#else
#	include ECSACT_ENTT_RUNTIME_USER_HEADER
#endif

#include "runtime.hh"

#ifndef ECSACT_ENTT_RUNTIME_PACKAGE
# error ECSACT_ENTT_RUNTIME_PACKAGE Must be defined with the fully qualified \
				meta package struct. 
#endif

namespace ecsact_entt_rt {
	extern ecsact::entt::runtime<ECSACT_ENTT_RUNTIME_PACKAGE> runtime;
}
