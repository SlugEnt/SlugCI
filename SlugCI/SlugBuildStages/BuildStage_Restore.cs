using System;
using System.Collections.Generic;
using System.Text;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
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

		/// <summary>
		/// Run Restore Process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			DotNetRestoreSettings settings = new DotNetRestoreSettings();
			settings.ProjectFile = CISession.Solution;
			settings.Verbosity = DotNetVerbosity.Minimal;
			IReadOnlyCollection<LineOut> outputs = DotNetTasks.DotNetRestore(settings);
			StageOutput.AddRange(outputs);
			return StageCompletionStatusEnum.Success;
		}
	}
}
