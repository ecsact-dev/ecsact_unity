#include "runtime.hh"

#ifndef ECSACT_ENTT_RUNTIME_USER_HEADER
#	error ECSACT_ENTT_RUNTIME_USER_HEADER must be defined
#else
#	include ECSACT_ENTT_RUNTIME_USER_HEADER
#endif

namespace ecsact_entt_rt {
  ecsact::entt::runtime<ECSACT_ENTT_RUNTIME_PACKAGE> runtime;
}
