using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

using Ecsact.UnitySync;

namespace Ecsact {

public class RegistryEntitySource : EntityGameObjectPool.EntitySource {
	EcsactRuntime runtime;
	private int registryId;

	internal RegistryEntitySource(int registryId, EcsactRuntime runtime) {
		this.runtime = runtime;
		this.registryId = registryId;
	}

	public override object GetComponent(int entityId, int componentId) {
		return runtime.core.GetComponent(
			registryId,
			entityId,
			componentId
		);
	}

	public override bool HasComponent(int entityId, int componentId) {
		return runtime.core.HasComponent(
			registryId,
			entityId,
			componentId
		);
	}
}

} // namespace Ecsact
