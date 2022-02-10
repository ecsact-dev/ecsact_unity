public abstract class EcsactCodegenPlugin {
	public abstract System.Threading.Tasks.Task Generate
		( EcsactPackage  pkg
		, string         outputPath
		);
}
