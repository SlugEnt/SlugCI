﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common.IO;
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
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			return StageCompletionStatusEnum.Success;
		}
	}
}
