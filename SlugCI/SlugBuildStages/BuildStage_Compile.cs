using System;
using System.Collections.Generic;
using System.Text;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Compile stage
	/// </summary>
	public class BuildStage_Compile : BuildStage {

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Compile (CISession ciSession) : base(BuildStageStatic.STAGE_COMPILE, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_RESTORE);
		}

	}
}
