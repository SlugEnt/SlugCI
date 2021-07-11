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
		public BuildStage_Clean (CISession ciSession) : base(BuildStageStatic.STAGE_CLEAN, ciSession) { }


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess() {
			IReadOnlyCollection<AbsolutePath> directoriesToClean = CISession.SolutionPath.GlobDirectories("**/bin", "**/obj");
			foreach (AbsolutePath dir in directoriesToClean)
			{
				FileSystemTasks.EnsureCleanDirectory(dir);
			}




			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		///   <p>The <c>dotnet clean</c> command cleans the output of the previous build. It's implemented as an <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets">MSBuild target</a>, so the project is evaluated when the command is run. Only the outputs created during the build are cleaned. Both intermediate <em>(obj)</em> and final output <em>(bin)</em> folders are cleaned.</p>
		///   <p>For more details, visit the <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/">official website</a>.</p>
		/// </summary>
		/// <remarks>
		///   <p>This is a <a href="http://www.nuke.build/docs/authoring-builds/cli-tools.html#fluent-apis">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p>
		///   <ul>
		///     <li><c>&lt;project&gt;</c> via <see cref="DotNetCleanSettings.Project"/></li>
		///     <li><c>--configuration</c> via <see cref="DotNetCleanSettings.Configuration"/></li>
		///     <li><c>--framework</c> via <see cref="DotNetCleanSettings.Framework"/></li>
		///     <li><c>--nologo</c> via <see cref="DotNetCleanSettings.NoLogo"/></li>
		///     <li><c>--output</c> via <see cref="DotNetCleanSettings.Output"/></li>
		///     <li><c>--runtime</c> via <see cref="DotNetCleanSettings.Runtime"/></li>
		///     <li><c>--verbosity</c> via <see cref="DotNetCleanSettings.Verbosity"/></li>
		///     <li><c>/property</c> via <see cref="DotNetCleanSettings.Properties"/></li>
		///   </ul>
		/// </remarks>
		private (BlockingCollection<ILineOut>, int) DotNetClean(DotNetCleanSettings toolSettings = null)
		{
			toolSettings = toolSettings ?? new DotNetCleanSettings();
			ProcessStartInfo processStartInfo = SlugCmdProcess.GetDefaultProcessSettings();
			ToolSettingsToProcessInfoConverter.Convert(toolSettings, processStartInfo);
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Dot Net Clean", processStartInfo);
			slugCmdProcess.Execute(DotNetClean_OutputProcessor);

			return (slugCmdProcess.Output, slugCmdProcess.ExitCode);
		}


		[CanBeNull]
		private LineOutColored? DotNetClean_OutputProcessor(EnumProcessOutputType type, string text)
		{
			if (text == null) return null;
			EnumProcessOutputType processType = type;
			LineOutColored lineOutColored;
			ReadOnlySpan<char> textSpan = text;

			if (type == EnumProcessOutputType.ProcessErr) return LineOutColored.Error(text);

			return LineOutColored.Normal(text);
		}

	}
}
