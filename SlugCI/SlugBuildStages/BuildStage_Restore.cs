using System;
using System.Collections.Generic;
using System.Text;
using Slug.CI.NukeClasses;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Restore Stage
	/// </summary>
	public class BuildStage_Restore : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession"></param>
		public BuildStage_Restore (CISession ciSession) : base(BuildStageStatic.STAGE_RESTORE, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_CLEAN);
		}

	}
}
