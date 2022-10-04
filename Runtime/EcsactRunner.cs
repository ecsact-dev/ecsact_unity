using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

namespace Ecsact {
	[AddComponentMenu("")]
	public class EcsactRunner : MonoBehaviour {

		protected EcsactRuntimeDefaultRegistry? defReg;
		protected List<EcsactRuntime.EcsactAction> actionList = new();
		public Int32 registryId => defReg?.registryId ?? -1;

#if UNITY_EDITOR
		[NonSerialized]
		public int debugExecutionCountTotal = 0;
		[NonSerialized]
		public int debugExecutionTimeMs = 0;
#endif

		public ExecuteOptions executeOptions;
		public struct ExecuteOptions {

			private List<EcsactRuntime.EcsactAction> actions;

			internal ExecuteOptions
				( EcsactRuntimeDefaultRegistry reg
				, ref List<EcsactRuntime.EcsactAction> actionList
				)
			{
				actions = actionList;
			}

			public void PushAction<T>
				( T action
				) where T : Ecsact.Action
			{
				var actionId = Ecsact.Util.GetActionID<T>();
				var actionPtr = Marshal.AllocHGlobal(Marshal.SizeOf(action));
				Marshal.StructureToPtr(action, actionPtr, false);
				var ecsAction = new EcsactRuntime.EcsactAction {
					actionId = actionId,
					actionData = actionPtr
				};
				actions.Add(ecsAction);
			}
		};

		protected static void OnRuntimeLoad<ComponentT>
			( EcsactRuntimeDefaultRegistry.RunnerType runnerType
			, string name
			) where ComponentT : EcsactRunner
		{

			var defReg = Ecsact.Defaults.Runner.defReg;
			defReg.executeOptions = new EcsactRuntime.ExecutionOptions{};

			var gameObjectName = name;
			if(!string.IsNullOrWhiteSpace(defReg.registryName)) {
				gameObjectName += " - " + defReg.registryName;
			}

			var gameObject = new GameObject(gameObjectName);
			var runner = gameObject.AddComponent(
				typeof(ComponentT)
			) as EcsactRunner;
			if(runner is not null) {
				runner.defReg = defReg;

				runner.executeOptions = new ExecuteOptions(
					defReg,
					ref runner.actionList
				);
				DontDestroyOnLoad(gameObject);
				Ecsact.Defaults.Runner = runner;		
			} else {
				throw new Exception("Runner is not valid");
			}

		}

		protected void AddActionsToReg() {
			var actionsArray = actionList.ToArray();

			defReg!.executionOptions.actions = actionsArray;
			defReg!.executionOptions.actionsLength = actionsArray.Length;
		}

		protected void FreeActions() {
			foreach(var ecsAction in actionList) {
				Marshal.FreeHGlobal(ecsAction.actionData);
			}
			actionList.Clear();
		}

		protected void Execute() {
			if(defReg == null) return;
			UnityEngine.Debug.Assert(defReg.registryId != -1);

#if UNITY_EDITOR
			var executionTimeWatch = Stopwatch.StartNew();
#endif

			if(actionList.Count > 0) {
				AddActionsToReg();
			}

			try {
				Ecsact.Defaults.Registry.ExecuteSystems(
					registryId: defReg.registryId,
					executionCount: 1,
					new EcsactRuntime.ExecutionOptions[]{defReg.executionOptions}
				);
			} finally {
				FreeActions();
			}

#if UNITY_EDITOR
			executionTimeWatch.Stop();
			debugExecutionTimeMs = (int)executionTimeWatch.ElapsedMilliseconds;
			debugExecutionCountTotal += 1;
#endif

			defReg.executionOptions = new EcsactRuntime.ExecutionOptions();
		}

		void Start() {
			gameObject.name += $" ({defReg!.registryId})";
		}
	}
}
