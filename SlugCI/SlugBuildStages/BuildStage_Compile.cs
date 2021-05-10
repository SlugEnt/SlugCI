using System;
using System.Collections.Generic;
using System.Text;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
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


		/// <summary>
		/// Run Compile process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			DotNetBuildSettings dotNetBuildSettings = new DotNetBuildSettings()
			{
				ProjectFile = CISession.Solution,
				NoRestore = true
			};
			dotNetBuildSettings.SetProjectFile(CISession.Solution);
			dotNetBuildSettings.SetFileVersion("9.4.5");
			dotNetBuildSettings.SetVerbosity(DotNetVerbosity.Diagnostic);
			dotNetBuildSettings.EnableNoRestore();

			IReadOnlyCollection<Output> out1 = DotNetTasks.DotNetBuild(dotNetBuildSettings);

			return StageCompletionStatusEnum.Success;
		}
	}
}
