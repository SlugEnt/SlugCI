using Slug.CI.NukeClasses;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// Marks the last stage to be run.
	/// </summary>
	class BuildStage_Final: BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Final (CISession ciSession) : base(BuildStageStatic.STAGE_FINAL, ciSession) {
			AddPredecessor(BuildStageStatic.STAGE_TYPEWRITER_PUBLISH);
		}


		/// <summary>
		/// Empty method.
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			return StageCompletionStatusEnum.Success;
		}
	}
}
