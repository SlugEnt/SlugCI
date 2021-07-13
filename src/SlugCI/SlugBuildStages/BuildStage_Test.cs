using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;
using StringExtensions;
using YamlDotNet.Serialization.NodeDeserializers;
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
		protected override StageCompletionStatusEnum ExecuteProcess() {
			if ( CISession.SkipTests ) return StageCompletionStatusEnum.Skipped;

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


			(BlockingCollection<ILineOut> outputs, int exitCode) = DotNetTest(settings);
			StageOutput.AddRange(outputs);
			if ( exitCode > 0 && CISession.FailedUnitTestsOkay ) {
				AOT_Warning("One or more unit tests failed.  HOWEVER, the Failed Unit Tests Okay flag was set, so this is not stopping the CI process");
				return StageCompletionStatusEnum.Warning;
			}

			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		///   <p>The <c>dotnet test</c> command is used to execute unit tests in a given project. Unit tests are console application projects that have dependencies on the unit test framework (for example, MSTest, NUnit, or xUnit) and the dotnet test runner for the unit testing framework. These are packaged as NuGet packages and are restored as ordinary dependencies for the project.</p>
		///   <p>For more details, visit the <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/">official website</a>.</p>
		/// </summary>
		/// <remarks>
		///   <p>This is a <a href="http://www.nuke.build/docs/authoring-builds/cli-tools.html#fluent-apis">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p>
		///   <ul>
		///     <li><c>&lt;projectFile&gt;</c> via <see cref="DotNetTestSettings.ProjectFile"/></li>
		///     <li><c>--</c> via <see cref="DotNetTestSettings.RunSettings"/></li>
		///     <li><c>--blame</c> via <see cref="DotNetTestSettings.BlameMode"/></li>
		///     <li><c>--collect</c> via <see cref="DotNetTestSettings.DataCollector"/></li>
		///     <li><c>--configuration</c> via <see cref="DotNetTestSettings.Configuration"/></li>
		///     <li><c>--diag</c> via <see cref="DotNetTestSettings.DiagnosticsFile"/></li>
		///     <li><c>--disable-parallel</c> via <see cref="DotNetTestSettings.DisableParallel"/></li>
		///     <li><c>--filter</c> via <see cref="DotNetTestSettings.Filter"/></li>
		///     <li><c>--force</c> via <see cref="DotNetTestSettings.Force"/></li>
		///     <li><c>--force-evaluate</c> via <see cref="DotNetTestSettings.ForceEvaluate"/></li>
		///     <li><c>--framework</c> via <see cref="DotNetTestSettings.Framework"/></li>
		///     <li><c>--ignore-failed-sources</c> via <see cref="DotNetTestSettings.IgnoreFailedSources"/></li>
		///     <li><c>--list-tests</c> via <see cref="DotNetTestSettings.ListTests"/></li>
		///     <li><c>--lock-file-path</c> via <see cref="DotNetTestSettings.LockFilePath"/></li>
		///     <li><c>--locked-mode</c> via <see cref="DotNetTestSettings.LockedMode"/></li>
		///     <li><c>--logger</c> via <see cref="DotNetTestSettings.Logger"/></li>
		///     <li><c>--no-build</c> via <see cref="DotNetTestSettings.NoBuild"/></li>
		///     <li><c>--no-cache</c> via <see cref="DotNetTestSettings.NoCache"/></li>
		///     <li><c>--no-dependencies</c> via <see cref="DotNetTestSettings.NoDependencies"/></li>
		///     <li><c>--no-restore</c> via <see cref="DotNetTestSettings.NoRestore"/></li>
		///     <li><c>--output</c> via <see cref="DotNetTestSettings.Output"/></li>
		///     <li><c>--packages</c> via <see cref="DotNetTestSettings.PackageDirectory"/></li>
		///     <li><c>--results-directory</c> via <see cref="DotNetTestSettings.ResultsDirectory"/></li>
		///     <li><c>--runtime</c> via <see cref="DotNetTestSettings.Runtime"/></li>
		///     <li><c>--settings</c> via <see cref="DotNetTestSettings.SettingsFile"/></li>
		///     <li><c>--source</c> via <see cref="DotNetTestSettings.Sources"/></li>
		///     <li><c>--test-adapter-path</c> via <see cref="DotNetTestSettings.TestAdapterPath"/></li>
		///     <li><c>--use-lock-file</c> via <see cref="DotNetTestSettings.UseLockFile"/></li>
		///     <li><c>--verbosity</c> via <see cref="DotNetTestSettings.Verbosity"/></li>
		///     <li><c>/property</c> via <see cref="DotNetTestSettings.Properties"/></li>
		///   </ul>
		/// </remarks>
		private (BlockingCollection<ILineOut>, int) DotNetTest(DotNetTestSettings toolSettings = null)
		{
			toolSettings = toolSettings ?? new DotNetTestSettings();
			ProcessStartInfo processStartInfo = SlugCmdProcess.GetDefaultProcessSettings();
			ToolSettingsToProcessInfoConverter.Convert(toolSettings, processStartInfo);
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Dot Net Test", processStartInfo);
			slugCmdProcess.Execute(DotNetTest_OutputProcessor);

			return (slugCmdProcess.Output, slugCmdProcess.ExitCode);
		}



		[CanBeNull]
		private LineOutColored? DotNetTest_OutputProcessor(EnumProcessOutputType type, string text)
		{
			if (text == null) return null;
			EnumProcessOutputType processType = type;
			LineOutColored lineOutColored;
			ReadOnlySpan<char> textSpan = text;

			if (type == EnumProcessOutputType.ProcessErr) return LineOutColored.Error(text);


			// Find index of the actual message part:
			// First 2 columns are blank
			if (StringExtension.SpanSearcherContains(textSpan, "Failed", 2, 9)) return LineOutColored.LogicError(text);
			if (StringExtension.SpanSearcherContains(textSpan, "Failed!", 0, 9)) return LineOutColored.Error(text);
			if (StringExtension.SpanSearcherContains(textSpan, "Passed!", 0, 9)) return LineOutColored.Success(text);
			return LineOutColored.Normal(text);
		}
	}
}
