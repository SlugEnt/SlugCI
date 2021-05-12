using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;

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
		/// Run Compile process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			if ( !CISession.SlugCIConfigObj.UseCodeCoverage ) {
				Logger.Info("Code Coverage is not enabled for this solution.");
				return StageCompletionStatusEnum.Skipped;
			}

			FileSystemTasks.EnsureExistingDirectory(CISession.CoveragePath);
			ReportGeneratorTasks.ReportGenerator(r => r.SetTargetDirectory(CISession.CoveragePath)
			                                           .SetProcessWorkingDirectory(CISession.CoveragePath)
			                                           .SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
			                                           .SetReports("coverage.cobertura.xml")
			                                           .SetProcessToolPath("reportgenerator"));

			AbsolutePath coverageFile = CISession.CoveragePath / "index.html";
			Process.Start(@"cmd.exe ", @"/c " + coverageFile);


			return StageCompletionStatusEnum.Success;
		}
	}
}
