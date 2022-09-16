using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

using Ecsact.UnitySync;

public class RegistryEntitySource : EntityGameObjectPool.EntitySource {
    EcsactRuntime runtime;
    private int registryId;

    internal RegistryEntitySource(int registryId) {
        runtime = EcsactRuntime.GetOrLoadDefault();
        registryId = registryId;
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
                UnityEngine.Debug.Log(monoStr);
                throw new Exception("Unity Sync: Incorrectly typed Monobehaviour");
            } else {
                UnityEngine.Debug.Log("Behaviour registered");
                UnitySyncMonoBehaviours.RegisterMonoBehaviourType(
                    type
                );
            }

        }
    }
}
