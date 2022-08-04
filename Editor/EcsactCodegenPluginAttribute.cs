using System;

[AttributeUsage(AttributeTargets.Class)]
public class EcsactCodegenPluginAttribute : Attribute {
	public string name = "";
	public string extname = "";
}
