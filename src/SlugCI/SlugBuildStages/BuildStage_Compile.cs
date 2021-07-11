using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;
using StringExtensions;

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
			PredecessorList.Add(BuildStageStatic.STAGE_CALCVERSION);
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
				NoRestore = true,
				PropertiesInternal = new Dictionary<string, object>(),
			};

			dotNetBuildSettings = dotNetBuildSettings.SetProjectFile(CISession.Solution)
			                                         .SetConfiguration(CISession.CompileConfig)
			                                         .SetVerbosity(DotNetVerbosity.Minimal)
			                                         .EnableNoRestore()
			                                         .SetAssemblyVersion(CISession.VersionInfo.AssemblyVersion)
			                                         .SetVersion(CISession.VersionInfo.AssemblyVersion)
			                                         .SetInformationalVersion(CISession.VersionInfo.InformationalVersion)
			                                         .SetFileVersion(CISession.VersionInfo.FileVersion);

			(BlockingCollection<ILineOut> outputs, int exitCode) = DotNetBuild(dotNetBuildSettings);
			StageOutput.AddRange(outputs);
			ControlFlow.Assert(exitCode == 0, "Process DotNetBuild failed");


			AOT_NewLine();
			AOT_Info("Compilation Status:");
			
			foreach ( SlugCIProject project in CISession.SlugCIConfigObj.Projects ) {
				foreach ( ILineOut lineOut in outputs ) {
					if ( lineOut.Text.StartsWith("  " + project.Name + " -> ") ) {
						AOT_Success("Compile Success:  " + lineOut.Text);
						project.Results.CompileSuccess = true;
						break;
					}
				}
			}
			
			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		///   <p>The <c>dotnet build</c> command builds the project and its dependencies into a set of binaries. The binaries include the project's code in Intermediate Language (IL) files with a <em>.dll</em> extension and symbol files used for debugging with a <em>.pdb</em> extension. A dependencies JSON file (<em>*.deps.json</em>) is produced that lists the dependencies of the application. A <em>.runtimeconfig.json</em> file is produced, which specifies the shared runtime and its version for the application.</p><p>If the project has third-party dependencies, such as libraries from NuGet, they're resolved from the NuGet cache and aren't available with the project's built output. With that in mind, the product of <c>dotnet build</c>d isn't ready to be transferred to another machine to run. This is in contrast to the behavior of the .NET Framework in which building an executable project (an application) produces output that's runnable on any machine where the .NET Framework is installed. To have a similar experience with .NET Core, you use the <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish"><c>dotnet publish</c></a> command. For more information, see <a href="https://docs.microsoft.com/en-us/dotnet/core/deploying/index">.NET Core Application Deployment</a>.</p><p>Building requires the <em>project.assets.json</em> file, which lists the dependencies of your application. The file is created <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore"><c>dotnet restore</c></a> is executed. Without the assets file in place, the tooling cannot resolve reference assemblies, which will result in errors. With .NET Core 1.x SDK, you needed to explicitily run the <c>dotnet restore</c> before running <c>dotnet build</c>. Starting with .NET Core 2.0 SDK, <c>dotnet restore</c> runs implicitily when you run <c>dotnet build</c>. If you want to disable implicit restore when running the build command, you can pass the <c>--no-restore</c> option.</p><p><c>dotnet build</c> uses MSBuild to build the project; thus, it supports both parallel and incremental builds. Refer to <a href="https://docs.microsoft.com/visualstudio/msbuild/incremental-builds">Incremental Builds</a> for more information.</p><p>In addition to its options, the <c>dotnet build</c> command accepts MSBuild options, such as <c>/p</c> for setting properties or <c>/l</c> to define a logger. Learn more about these options in the <a href="https://docs.microsoft.com/visualstudio/msbuild/msbuild-command-line-reference">MSBuild Command-Line Reference</a>.</p>
		///   <p>For more details, visit the <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/">official website</a>.</p>
		/// </summary>
		/// <remarks>
		///   <p>This is a <a href="http://www.nuke.build/docs/authoring-builds/cli-tools.html#fluent-apis">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p>
		///   <ul>
		///     <li><c>&lt;projectFile&gt;</c> via <see cref="DotNetBuildSettings.ProjectFile"/></li>
		///     <li><c>--configuration</c> via <see cref="DotNetBuildSettings.Configuration"/></li>
		///     <li><c>--disable-parallel</c> via <see cref="DotNetBuildSettings.DisableParallel"/></li>
		///     <li><c>--force</c> via <see cref="DotNetBuildSettings.Force"/></li>
		///     <li><c>--force-evaluate</c> via <see cref="DotNetBuildSettings.ForceEvaluate"/></li>
		///     <li><c>--framework</c> via <see cref="DotNetBuildSettings.Framework"/></li>
		///     <li><c>--ignore-failed-sources</c> via <see cref="DotNetBuildSettings.IgnoreFailedSources"/></li>
		///     <li><c>--lock-file-path</c> via <see cref="DotNetBuildSettings.LockFilePath"/></li>
		///     <li><c>--locked-mode</c> via <see cref="DotNetBuildSettings.LockedMode"/></li>
		///     <li><c>--no-cache</c> via <see cref="DotNetBuildSettings.NoCache"/></li>
		///     <li><c>--no-dependencies</c> via <see cref="DotNetBuildSettings.NoDependencies"/></li>
		///     <li><c>--no-incremental</c> via <see cref="DotNetBuildSettings.NoIncremental"/></li>
		///     <li><c>--no-restore</c> via <see cref="DotNetBuildSettings.NoRestore"/></li>
		///     <li><c>--nologo</c> via <see cref="DotNetBuildSettings.NoLogo"/></li>
		///     <li><c>--output</c> via <see cref="DotNetBuildSettings.OutputDirectory"/></li>
		///     <li><c>--packages</c> via <see cref="DotNetBuildSettings.PackageDirectory"/></li>
		///     <li><c>--runtime</c> via <see cref="DotNetBuildSettings.Runtime"/></li>
		///     <li><c>--source</c> via <see cref="DotNetBuildSettings.Sources"/></li>
		///     <li><c>--use-lock-file</c> via <see cref="DotNetBuildSettings.UseLockFile"/></li>
		///     <li><c>--verbosity</c> via <see cref="DotNetBuildSettings.Verbosity"/></li>
		///     <li><c>--version-suffix</c> via <see cref="DotNetBuildSettings.VersionSuffix"/></li>
		///     <li><c>/logger</c> via <see cref="DotNetBuildSettings.Loggers"/></li>
		///     <li><c>/noconsolelogger</c> via <see cref="DotNetBuildSettings.NoConsoleLogger"/></li>
		///     <li><c>/property</c> via <see cref="DotNetBuildSettings.Properties"/></li>
		///   </ul>
		/// </remarks>
		private (BlockingCollection<ILineOut>, int) DotNetBuild(DotNetBuildSettings toolSettings = null)
		{
			toolSettings = toolSettings ?? new DotNetBuildSettings();
			ProcessStartInfo processStartInfo = SlugCmdProcess.GetDefaultProcessSettings();
			ToolSettingsToProcessInfoConverter.Convert(toolSettings, processStartInfo);
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Dot Net Build", processStartInfo);
			slugCmdProcess.Execute(DotNetBuild_OutputProcessor);

			return (slugCmdProcess.Output, slugCmdProcess.ExitCode);
		}


		[CanBeNull]
		private LineOutColored? DotNetBuild_OutputProcessor (EnumProcessOutputType type, string text) {
			if ( text == null ) return null;
			EnumProcessOutputType processType = type;
			LineOutColored lineOutColored;
			ReadOnlySpan<char> textSpan = text;

			if ( type == EnumProcessOutputType.ProcessErr ) return LineOutColored.Error(text);

			// Find index of the actual message part:
			// The plus 17 is arbitrary as the src path,. still has a project path and source filename as well as line and column number entries
			int compileMsgStart = CISession.SourceDirectory.Length + 17;

			if ( text.Length > compileMsgStart ) {
				int index = text.IndexOf(": ", compileMsgStart);
				if ( index > 0 ) {
					if ( StringExtension.SpanSearcherContains(textSpan, "error", index + 2, index + 10) ) return LineOutColored.LogicError(text);
					if ( StringExtension.SpanSearcherContains(textSpan, "warning", index + 2, index + 10 + 50) ) return LineOutColored.Warning(text);
				}
			}
		return LineOutColored.Normal(text);
		}
	}
}
