using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Build.Construction;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;
using StringExtensions;
using static Nuke.Common.IO.FileSystemTasks;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Nuget Pack Step
	/// </summary>
	public class BuildStage_Pack : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession"></param>
		public BuildStage_Pack(CISession ciSession) : base(BuildStageStatic.STAGE_PACK, ciSession)
		{
			PredecessorList.Add(BuildStageStatic.STAGE_GITCOMMIT);
		}


		/// <summary>
		/// Run Pack Process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			CISession.OutputDirectory.GlobFiles("*.nupkg", "*symbols.nupkg").ForEach(DeleteFile);

			DotNetPackSettings settings;

			foreach ( SlugCIProject project in CISession.Projects ) {
				AddOutputText("Project: " + project.Name, OutputType.Std);
				if (project.Deploy == SlugCIDeployMethod.Nuget || project.Deploy == SlugCIDeployMethod.Tool)
					AddOutputText("  --> Is Nuget Packable.", OutputType.Info);
				else AddOutputText("  --> Is Not Nuget Packable.", OutputType.Info);

				if ( project.Deploy != SlugCIDeployMethod.Nuget && project.Deploy != SlugCIDeployMethod.Tool ) {continue;}
				settings = new DotNetPackSettings()
				{
					Project = project.VSProject.Path,
					OutputDirectory = CISession.OutputDirectory,
					IncludeSymbols = true,
					NoRestore = true,
					Verbosity = DotNetVerbosity.Minimal,
					PropertiesInternal = new Dictionary<string, object>(),
				};

				settings = settings.SetFileVersion(CISession.VersionInfo.FileVersion)
				                   .SetAssemblyVersion(CISession.VersionInfo.AssemblyVersion)
				                   .SetConfiguration(CISession.CompileConfig)
				                   .SetInformationalVersion(CISession.VersionInfo.InformationalVersion)
				                   .SetVersion(CISession.VersionInfo.SemVersionAsString);

				// We might need to override package name if this is an alpha or beta build.
				if ( project.Deploy == SlugCIDeployMethod.Tool ) {
					settings.SetPackageId(project.PackageId + "-" + CISession.PublishTarget.ToString());
				}


				(BlockingCollection<ILineOut> output, int exitCode) = DotNetPack(settings);
				StageOutput.AddRange(output);
				if ( exitCode != 0 ) {
					AOT_Error("DotNetPack returned non-zero exit code: " + exitCode);
					project.Results.PackedSuccess = false;
				}
				else {
					// See if successfully created package file
					string searchName = project.AssemblyName + "." + CISession.VersionInfo.SemVersionAsString + ".nupkg";
					var matchingvalues =
						output.Where(outputVal => (outputVal.Text.Contains("Successfully created package") && (outputVal.Text.Contains(searchName))));
					if ( matchingvalues.Count() == 1 ) { project.Results.PackedSuccess = true; }
					else
						project.Results.PackedSuccess = false;
				}

				// We set published to false here, as we can't really do it in the publish step
				project.Results.PublishedSuccess = false;
			}

			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		///   <p>The <c>dotnet pack</c> command builds the project and creates NuGet packages. The result of this command is a NuGet package. If the <c>--include-symbols</c> option is present, another package containing the debug symbols is created.</p><p>NuGet dependencies of the packed project are added to the <em>.nuspec</em> file, so they're properly resolved when the package is installed. Project-to-project references aren't packaged inside the project. Currently, you must have a package per project if you have project-to-project dependencies.</p><p>By default, <c>dotnet pack</c> builds the project first. If you wish to avoid this behavior, pass the <c>--no-build</c> option. This is often useful in Continuous Integration (CI) build scenarios where you know the code was previously built.</p><p>You can provide MSBuild properties to the <c>dotnet pack</c> command for the packing process. For more information, see <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/csproj#nuget-metadata-properties">NuGet metadata properties</a> and the <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference">MSBuild Command-Line Reference</a>.</p>
		///   <p>For more details, visit the <a href="https://docs.microsoft.com/en-us/dotnet/core/tools/">official website</a>.</p>
		/// </summary>
		/// <remarks>
		///   <p>This is a <a href="http://www.nuke.build/docs/authoring-builds/cli-tools.html#fluent-apis">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p>
		///   <ul>
		///     <li><c>&lt;project&gt;</c> via <see cref="DotNetPackSettings.Project"/></li>
		///     <li><c>--configuration</c> via <see cref="DotNetPackSettings.Configuration"/></li>
		///     <li><c>--disable-parallel</c> via <see cref="DotNetPackSettings.DisableParallel"/></li>
		///     <li><c>--force</c> via <see cref="DotNetPackSettings.Force"/></li>
		///     <li><c>--force-evaluate</c> via <see cref="DotNetPackSettings.ForceEvaluate"/></li>
		///     <li><c>--ignore-failed-sources</c> via <see cref="DotNetPackSettings.IgnoreFailedSources"/></li>
		///     <li><c>--include-source</c> via <see cref="DotNetPackSettings.IncludeSource"/></li>
		///     <li><c>--include-symbols</c> via <see cref="DotNetPackSettings.IncludeSymbols"/></li>
		///     <li><c>--lock-file-path</c> via <see cref="DotNetPackSettings.LockFilePath"/></li>
		///     <li><c>--locked-mode</c> via <see cref="DotNetPackSettings.LockedMode"/></li>
		///     <li><c>--no-build</c> via <see cref="DotNetPackSettings.NoBuild"/></li>
		///     <li><c>--no-cache</c> via <see cref="DotNetPackSettings.NoCache"/></li>
		///     <li><c>--no-dependencies</c> via <see cref="DotNetPackSettings.NoDependencies"/></li>
		///     <li><c>--no-restore</c> via <see cref="DotNetPackSettings.NoRestore"/></li>
		///     <li><c>--nologo</c> via <see cref="DotNetPackSettings.NoLogo"/></li>
		///     <li><c>--output</c> via <see cref="DotNetPackSettings.OutputDirectory"/></li>
		///     <li><c>--packages</c> via <see cref="DotNetPackSettings.PackageDirectory"/></li>
		///     <li><c>--runtime</c> via <see cref="DotNetPackSettings.Runtime"/></li>
		///     <li><c>--serviceable</c> via <see cref="DotNetPackSettings.Serviceable"/></li>
		///     <li><c>--source</c> via <see cref="DotNetPackSettings.Sources"/></li>
		///     <li><c>--use-lock-file</c> via <see cref="DotNetPackSettings.UseLockFile"/></li>
		///     <li><c>--verbosity</c> via <see cref="DotNetPackSettings.Verbosity"/></li>
		///     <li><c>--version-suffix</c> via <see cref="DotNetPackSettings.VersionSuffix"/></li>
		///     <li><c>/property</c> via <see cref="DotNetPackSettings.Properties"/></li>
		///   </ul>
		/// </remarks>
		private (BlockingCollection<ILineOut>, int) DotNetPack(DotNetPackSettings toolSettings = null)
		{

			toolSettings = toolSettings ?? new DotNetPackSettings();
			ProcessStartInfo processStartInfo = SlugCmdProcess.GetDefaultProcessSettings();
			ToolSettingsToProcessInfoConverter.Convert(toolSettings, processStartInfo);
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Dot Net Pack", processStartInfo);
			slugCmdProcess.Execute(DotNetPack_OutputProcessor);

			return (slugCmdProcess.Output, slugCmdProcess.ExitCode);
		}



		[CanBeNull]
		private LineOutColored? DotNetPack_OutputProcessor(EnumProcessOutputType type, string text)
		{
			if (text == null) return null;
			EnumProcessOutputType processType = type;
			LineOutColored lineOutColored;
			ReadOnlySpan<char> textSpan = text;

			if (type == EnumProcessOutputType.ProcessErr) return LineOutColored.Error(text);

			// Find index of the actual message part:
			// The plus 17 is arbitrary as the src path,. still has a project path and source filename as well as line and column number entries
/*			int compileMsgStart = CISession.SourceDirectory.Length + 17;

			if (text.Length > compileMsgStart)
			{
				int index = text.IndexOf(": ", compileMsgStart);
				if (index > 0)
				{
					if (StringExtension.SpanSearcherContains(textSpan, "error", index + 2, index + 10)) return LineOutColored.LogicError(text);
					if (StringExtension.SpanSearcherContains(textSpan, "warning", index + 2, index + 10 + 50)) return LineOutColored.Warning(text);
				}
			}
*/
			return LineOutColored.Normal(text);
		}

	}
}
