public abstract class EcsIdlCodegenPlugin {
	public abstract System.Threading.Tasks.Task Generate
		( EcsIdlPackage  pkg
		, string         outputPath
		);
}
