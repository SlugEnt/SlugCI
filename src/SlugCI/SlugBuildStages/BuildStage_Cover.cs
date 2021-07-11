using System.Collections.Generic;
using System.Diagnostics;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.ReportGenerator;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Compile stage
	/// </summary>
	public class BuildStage_Cover : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Cover(CISession ciSession) : base(BuildStageStatic.STAGE_COVER, ciSession)
		{
			// TODO Need to figure out what to do here... It could be PUBLISH OR PUBLISH TEST or do we do a post processing Execution plan...?
			PredecessorList.Add(BuildStageStatic.STAGE_PUBLISH);
		}


		/// <summary>
		/// Run the coverage tool
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			if ( !CISession.SlugCIConfigObj.UseCodeCoverage ) {
				Logger.Info("Code Coverage is not enabled for this solution.");
				return StageCompletionStatusEnum.Skipped;
			}

			FileSystemTasks.EnsureExistingDirectory(CISession.CoveragePath);
			ReportGeneratorSettings settings = new ReportGeneratorSettings()
			{
				ProcessWorkingDirectory = CISession.CoveragePath,
				TargetDirectory = CISession.CoveragePath,
				ProcessToolPath = "reportgenerator",
				ReportsInternal = new List<string>() {"coverage.cobertura.xml"},
				ReportTypesInternal = new List<ReportTypes>()
				{
					ReportTypes.Badges,
					ReportTypes.HtmlInline
				},
			};

			AbsolutePath coverageFile = CISession.CoveragePath / "index.html";
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Code Coverage",SlugCmdProcess.GetDefaultProcessSettings());
			Process.Start(@"cmd.exe ", @"/c " + coverageFile);

			// TODO Await completion

			return StageCompletionStatusEnum.Success;
		}
	}
}
