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
	/// The DotNet Cleaning stage
	/// </summary>
	class BuildStage_Clean : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Clean (CISession ciSession) : base(BuildStageStatic.STAGE_CLEAN, ciSession) { }


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess() {
			IReadOnlyCollection<AbsolutePath> directoriesToClean = CISession.SolutionPath.GlobDirectories("**/bin", "**/obj");
			foreach (AbsolutePath dir in directoriesToClean)
			{
				FileSystemTasks.EnsureCleanDirectory(dir);
			}

			return StageCompletionStatusEnum.Success;
		}
	}
}
