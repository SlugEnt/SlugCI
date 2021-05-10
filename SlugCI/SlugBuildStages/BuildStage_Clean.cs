using System;
using System.Collections.Generic;
using System.Text;
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
		public BuildStage_Clean (CISession ciSession) : base(BuildStageStatic.STAGE_CLEAN, ciSession) {}
	}
}
