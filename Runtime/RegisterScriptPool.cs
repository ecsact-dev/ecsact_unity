using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

using Ecsact.UnitySync;

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

public class RegisterScriptPool : MonoBehaviour {

    void Awake() {
        var settings = EcsactRuntimeSettings.Get();
        var monobehaviours = settings.unitySyncScripts;

        foreach(var monoStr in monobehaviours) {

            var type = Type.GetType(monoStr + ",Assembly-CSharp");
            if(type == null) {
                throw new Exception(
                    "Unity Sync: Monobehaviour " + monoStr + " not found "
                );
            } else {
                UnitySyncMonoBehaviours.RegisterMonoBehaviourType(
                    type
                );
            }

        }
    }
}
