using System.Collections.Generic;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Unit Test Runner 
	/// </summary>
	public class BuildStage_Test : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Test(CISession ciSession) : base(BuildStageStatic.STAGE_TEST, ciSession)
		{
			PredecessorList.Add(BuildStageStatic.STAGE_COMPILE);
		}


		/// <summary>
		/// Run Unit Test Runner Process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			FileSystemTasks.EnsureExistingDirectory(CISession.CoveragePath);

			DotNetTestSettings settings = new DotNetTestSettings()
			{
				ProjectFile = CISession.Solution,
				Configuration = CISession.CompileConfig,
				NoRestore = true,
				NoBuild = true,
				Verbosity = DotNetVerbosity.Minimal,
				ProcessLogOutput = true,
				ResultsDirectory = CISession.TestOutputPath,
				ProcessArgumentConfigurator = arguments => arguments.Add("/p:CollectCoverage={0}", true)
				                                                    .Add("", false)
				                                                    .Add("/p:CoverletOutput={0}/", CISession.CoveragePath)
				                                                    .Add("/p:CoverletOutputFormat={0}", "cobertura")
				                                                    .Add("/p:Threshold={0}", CISession.SlugCIConfigObj.CodeCoverageThreshold)
				                                                    .Add("/p:SkipAutoProps={0}", true)
				                                                    .Add("/p:ExcludeByAttribute={0}",
				                                                         "\"Obsolete%2cGeneratedCodeAttribute%2cCompilerGeneratedAttribute\"")
				                                                    .Add("/p:UseSourceLink={0}", true)

			};

			(IReadOnlyCollection<LineOutColored> testOutput, int processExitCode) =  DotNetTest(settings);
			StageOutput.AddRange(testOutput);

			if ( processExitCode != 0 ) return StageCompletionStatusEnum.Failure;
			return StageCompletionStatusEnum.Success;
		}
	}
}
