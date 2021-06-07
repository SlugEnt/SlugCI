using System.Collections.Generic;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Slug.CI.NukeClasses;
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

			DotNetPackSettings settings;// = new DotNetPackSettings()

			foreach ( SlugCIProject project in CISession.Projects ) {
				AddOutputText("Project: " + project.Name, OutputType.Std);
				AddOutputText("  --> Is Nuget Packable:  " + (project.Deploy == SlugCIDeployMethod.Nuget).ToString(), OutputType.Std);

				if ( project.Deploy != SlugCIDeployMethod.Nuget ) {continue;}
				settings = new DotNetPackSettings()
				{
					Project = project.VSProject.Path,
					OutputDirectory = CISession.OutputDirectory,
					IncludeSymbols = true,
					NoRestore = true,
					Verbosity = DotNetVerbosity.Minimal,
					PropertiesInternal = new Dictionary<string, object>(),
				};

				string version = CISession.VersionInfo.SemVersionAsString;
				settings = settings.SetFileVersion(CISession.VersionInfo.FileVersion)
				                   .SetAssemblyVersion(CISession.VersionInfo.AssemblyVersion)
				                   .SetConfiguration(CISession.CompileConfig)
				                   .SetInformationalVersion(CISession.VersionInfo.InformationalVersion)
				                   .SetVersion(CISession.VersionInfo.SemVersionAsString);

				IReadOnlyCollection<Output> output = DotNetTasks.DotNetPack(settings);
				StageOutput.AddRange(output);

				// See if successful.
				string searchName = project.AssemblyName + "." + CISession.VersionInfo.SemVersionAsString + ".nupkg";
				var matchingvalues =
					output.Where(outputVal => (outputVal.Text.Contains("Successfully created package") && (outputVal.Text.Contains(searchName))));
				if ( matchingvalues.Count() == 1 ) {
					project.Results.PackedSuccess = true;
				}
				else
					project.Results.PackedSuccess = false;
				// We set published to false here, as we can't really do it in the publish step
				project.Results.PublishedSuccess = false;
			}

			return StageCompletionStatusEnum.Success;

		}
	}
}
