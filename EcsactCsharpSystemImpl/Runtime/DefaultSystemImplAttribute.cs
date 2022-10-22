using System;
using System.Reflection;

namespace Ecsact {

/// <summary>The <c>Ecsact.DefaultSystemImpl</c> attribute is used to
/// mark a static function as a system implementation for the
/// <see cref="Ecsact.Defaults.Runtime"/>.</summary>
///
/// <example>
/// <code>
///   [Ecsact.DefaultSystemImpl(typeof(example.ExampleSystem))]
///   static void ExampleSystem(Ecsact.SystemExecutionContext ctx) {
///
///   }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class DefaultSystemImplAttribute : Attribute {
	public global::System.Int32 systemLikeId { get; private set; }

	public DefaultSystemImplAttribute(global::System.Type systemLikeType) {
		var idField =
			systemLikeType.GetField("id", BindingFlags.Static | BindingFlags.Public);

		systemLikeId = (Int32)idField.GetValue(null);
	}
}

} // namespace Ecsact
