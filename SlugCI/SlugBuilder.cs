using System;
using System.Collections.Generic;
using System.Text;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace SlugCI
{
	class SlugBuilder
	{
		public Solution Solution { get; set; }

		public GitRepository GitRepository { get; set; }

		public SlugBuilder () {

			Solution = SolutionSerializer.DeserializeFromFile<Solution>(@"C:\A_Dev\SlugEnt\NukeTestControl\src\NukeTestControl.sln");
			GitRepository = GitRepository.FromLocalDirectory(@"C:\A_Dev\SlugEnt\NukeTestControl\");

		}


		public bool Clean () {
			AbsolutePath SourceDirectory = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\src\Printer";
			IReadOnlyCollection<AbsolutePath> directoriesToClean = SourceDirectory.GlobDirectories("**/bin", "**/obj");

			//TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
			foreach ( AbsolutePath dir in directoriesToClean ) {
				FileSystemTasks.EnsureCleanDirectory(dir);
			}
			
			return true;
		}


		/// <summary>
		/// Performs a restore which ensures all the packages on the compiling pc are current with the expected versions in solution.
		/// This DOES NOT upgrade packages.
		/// </summary>
		/// <returns></returns>
		public bool RestoreNugetPackages () {
			DotNetRestoreSettings settings = new DotNetRestoreSettings();
			settings.ProjectFile = Solution;
			IReadOnlyCollection<Output> outputs = DotNetTasks.DotNetRestore(settings);
			return true;
		}


		public bool Compile () {
			DotNetBuildSettings dotNetBuildSettings = new DotNetBuildSettings()
			{
				ProjectFile = Solution,
				NoRestore = true
			};
			dotNetBuildSettings.SetProjectFile(Solution);
			dotNetBuildSettings.SetFileVersion("9.4.5");
			dotNetBuildSettings.SetVerbosity(DotNetVerbosity.Diagnostic);
			dotNetBuildSettings.EnableNoRestore();

			IReadOnlyCollection<Output> out1 = DotNetTasks.DotNetBuild(dotNetBuildSettings);
			return true;
		}



		public bool Pack () {
			//OutputDirectory.GlobFiles("*.nupkg", "*symbols.nupkg").ForEach(DeleteFile);

			DotNetPackSettings settings = new DotNetPackSettings();
			foreach ( Project x in Solution.AllProjects ) {
				settings.Project = x.Path;
				//settings.SetProject(x.Path);
				settings.OutputDirectory = @"C:\A_Dev\SlugEnt\NukeTestControl\artifacts";
				settings.IncludeSymbols = true;
				settings.NoRestore = true;
				settings.SetFileVersion("4.5.6");
				IReadOnlyCollection<Output> output = DotNetTasks.DotNetPack(settings);
			}

			return true;
			/*
			IReadOnlyCollection<Output> output = DotNetTasks.DotNetPack(_ => _.SetProject(Solution.GetProject(fullName))
			                                                      .SetOutputDirectory(OutputDirectory)
			                                                      .SetConfiguration(Configuration)
			                                                      .SetIncludeSymbols(true)
			                                                      .SetAssemblyVersion(_gitProcessor.GitVersion.AssemblySemVer)
			                                                      .SetFileVersion(_gitProcessor.GitVersion.AssemblySemFileVer)
			                                                      .SetInformationalVersion(_gitProcessor.InformationalVersion)
			                                                      .SetVersion(_gitProcessor.SemVersionNugetCompatible));
			*/
		}




	}
}
