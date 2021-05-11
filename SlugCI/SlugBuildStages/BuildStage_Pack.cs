using System;
using System.Collections.Generic;
using System.Text;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;

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
			PredecessorList.Add(BuildStageStatic.STAGE_TEST);
		}


		/// <summary>
		/// Run Pack Process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			// TODO Is this necessry or can it be deleted.
			//OutputDirectory.GlobFiles("*.nupkg", "*symbols.nupkg").ForEach(DeleteFile);

			DotNetPackSettings settings = new DotNetPackSettings();
			foreach (Nuke.Common.ProjectModel.Project x in CISession.Solution.AllProjects)
			{
				
				settings.Project = x.Path;
				//settings.SetProject(x.Path);
				settings.OutputDirectory = CISession.OutputDirectory;
				settings.IncludeSymbols = true;
				settings.NoRestore = true;
				settings.Verbosity = DotNetVerbosity.Diagnostic;
				// TODO - FIX version
				settings.SetFileVersion("4.5.6");
				IReadOnlyCollection<Output> output = DotNetTasks.DotNetPack(settings);
			}

			return StageCompletionStatusEnum.Success;


			/* Original SlugNuke Code
			 		                     // Build the necessary packages 
		                     foreach ( NukeConf.Project project in CustomNukeSolutionConfig.Projects ) {
			                     if ( project.Deploy == CustomNukeDeployMethod.Nuget ) {
				                     string fullName = SourceDirectory / project.Name / project.Name + ".csproj";
				                     IReadOnlyCollection<Output> output = DotNetPack(_ => _.SetProject(Solution.GetProject(fullName))
				                                                                           .SetOutputDirectory(OutputDirectory)
				                                                                           .SetConfiguration(Configuration)
				                                                                           .SetIncludeSymbols(true)
				                                                                           .SetAssemblyVersion(_gitProcessor.GitVersion.AssemblySemVer)
				                                                                           .SetFileVersion(_gitProcessor.GitVersion.AssemblySemFileVer)
				                                                                           .SetInformationalVersion(_gitProcessor.InformationalVersion)
				                                                                           .SetVersion(_gitProcessor.SemVersionNugetCompatible));
				                     foreach ( Output item in output ) {
					                     if ( item.Text.Contains("Successfully created package") ) {
						                     string file = item.Text.Substring(item.Text.IndexOf("'") + 1);
						                     PublishResults.Add(new PublishResult(project.Name, project.Deploy.ToString(), file));
					                     }
				                     }
			                     }

		                     }
	                     });

			 */
		}
	}
}
