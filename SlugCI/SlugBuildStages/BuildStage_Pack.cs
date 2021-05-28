using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
				if ( project.Deploy != SlugCIDeployMethod.Nuget ) continue;
				settings = new DotNetPackSettings()
				{
					Project = project.VSProject.Path,
					OutputDirectory = CISession.OutputDirectory,
					IncludeSymbols = true,
					NoRestore = true,
					Verbosity = DotNetVerbosity.Minimal,
					PropertiesInternal = new Dictionary<string, object>(),
				};

				string version = CISession.SemVersion.ToString();
				settings = settings.SetFileVersion(version)
				                   .SetAssemblyVersion(version)
				                   .SetConfiguration(CISession.CompileConfig)
				                   .SetInformationalVersion(version)
				                   .SetVersion(version);

				IReadOnlyCollection<Output> output = DotNetTasks.DotNetPack(settings);

				// See if successful.
				string searchName = project.AssemblyName + "." + CISession.SemVersion.ToString() + ".nupkg";
				var matchingvalues =
					output.Where(outputVal => (outputVal.Text.Contains("Successfully created package") && (outputVal.Text.Contains(searchName))));
				if ( matchingvalues.Count() == 1 ) {
					project.Results.PackedSuccess = true;
				}
			}

			return StageCompletionStatusEnum.Success;

		}
	}
}
