using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Slug.CI.NukeClasses;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// This stage commits the version bump and other changes to production...
	/// </summary>
	class BuildStage_GitCommit : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_GitCommit(CISession ciSession) : base(BuildStageStatic.STAGE_GITCOMMIT, ciSession) { }


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			CISession.GitProcessor.CommitSemVersionChanges(CISession.SemVersion);

			return StageCompletionStatusEnum.Success;
		}
	}
}
