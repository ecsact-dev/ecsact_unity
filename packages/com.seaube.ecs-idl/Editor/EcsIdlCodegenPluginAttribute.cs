using System;

[AttributeUsage(AttributeTargets.Class)]
public class EcsIdlCodegenPluginAttribute : Attribute {
	public string name = "";
	public string extname = "";
}
