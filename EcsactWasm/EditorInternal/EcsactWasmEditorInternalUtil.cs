namespace Ecsact.Editor.Internal {
	public static class EcsactWasmEditorInternalUtil {
		public delegate UnityEditor.Editor GetEditorDelegate();
		public static GetEditorDelegate GetEcsactWasmRuntimeSettingsEditor;
	}
}
