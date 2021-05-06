using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;

namespace Slug.CI
{
	class SlugBuilder
	{
		/// <summary>
		/// The Solution we are processing
		/// </summary>
		public Solution Solution { get; set; }


		/// <summary>
		/// The Git Repository for the Solution
		/// </summary>
		public GitRepository GitRepository { get; set; }


		/// <summary>
		/// The temporary storage path
		/// </summary>
		public AbsolutePath ArtifactPath { get;private set; }
		

		/// <summary>
		/// The root path of the git repository / parent of solution folders
		/// </summary>
		public AbsolutePath RootPath { get; private set; }


		/// <summary>
		/// Where coverage reports are located
		/// </summary>
		public AbsolutePath CoveragePath { get; private set; }


		/// <summary>
		/// Path of the folder containing the .sln file.
		/// </summary>
		public AbsolutePath SolutionPath { get; private set; }


		/// <summary>
		/// Test output location
		/// </summary>
		public AbsolutePath TestOutputPath { get; private set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public SlugBuilder () {

			Solution = SolutionSerializer.DeserializeFromFile<Solution>(@"C:\A_Dev\SlugEnt\NukeTestControl\src\NukeTestControl.sln");
			GitRepository = GitRepository.FromLocalDirectory(@"C:\A_Dev\SlugEnt\NukeTestControl\");

			// Set Path properties
			SolutionPath = Solution.Directory;
			CoveragePath = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\artifacts\Coverage";
			TestOutputPath = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\artifacts\Tests";
		}


		public bool Info () {
			/*
			Logger.Normal("Root =         " + RootDirectory.ToString());
			Logger.Normal("Source =       " + SourceDirectory.ToString());
			Logger.Normal("Tests:         " + TestsDirectory.ToString());
			Logger.Normal("Output:        " + OutputDirectory);
			Logger.Normal("Solution:      " + Solution.Path);

			Logger.Normal("Build Assemnbly Dir:       " + BuildAssemblyDirectory);
			Logger.Normal("Build Project Dir:         " + BuildProjectDirectory);
			Logger.Normal("NugetPackageConfigFile:    " + ToolPathResolver.NuGetPackagesConfigFile);
			//Logger.Normal("Executing Assembly Dir:    " + ToolPathResolver. .ExecutingAssemblyDirectory);
			Logger.Normal("Nuget Assets Config File:  " + ToolPathResolver.NuGetAssetsConfigFile);
			Logger.Normal();
			*/
			return true;
		}


		public bool Clean () {
			//AbsolutePath SourceDirectory = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\src\Printer";
			IReadOnlyCollection<AbsolutePath> directoriesToClean = SolutionPath.GlobDirectories("**/bin", "**/obj");

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


		public bool Test () {
			
			FileSystemTasks.EnsureExistingDirectory(CoveragePath);

			DotNetTestSettings settings = new DotNetTestSettings()
			{
				ProjectFile = Solution,
				NoRestore = true,
				NoBuild = true,
				ProcessLogOutput = true,
				ResultsDirectory = TestOutputPath,
				ProcessArgumentConfigurator = arguments => arguments.Add("/p:CollectCoverage={0}", true)
				                                                    .Add("", false)
				                                                    .Add("/p:CoverletOutput={0}/", CoveragePath)
				                                                    .Add("/p:CoverletOutputFormat={0}", "cobertura")
				                                                    //.Add("/p:Threshold={0}", SlugCIConfig.CodeCoverageThreshold)
				                                                    .Add("/p:Threshold={0}", 5)
				                                                    .Add("/p:SkipAutoProps={0}", true)
				                                                    .Add("/p:ExcludeByAttribute={0}",
				                                                         "\"Obsolete%2cGeneratedCodeAttribute%2cCompilerGeneratedAttribute\"")
				                                                    .Add("/p:UseSourceLink={0}", true)

		};
/*
			settings.SetProcessArgumentConfigurator(arguments => arguments.Add("/p:CollectCoverage={0}", true)
			                                                              .Add("", false)
			                                                              .Add("/p:CoverletOutput={0}/", coveragePath)
			                                                              .Add("/p:CoverletOutputFormat={0}", "cobertura")
																		  //.Add("/p:Threshold={0}", SlugCIConfig.CodeCoverageThreshold)
																		  .Add("/p:Threshold={0}", 5)
																		  .Add("/p:SkipAutoProps={0}", true)
			                                                              .Add("/p:ExcludeByAttribute={0}",
			                                                                   "\"Obsolete%2cGeneratedCodeAttribute%2cCompilerGeneratedAttribute\"")
			                                                              .Add("/p:UseSourceLink={0}", true));
*/
			DotNetTasks.DotNetTest(settings);
			return true;
			/*
			foreach (NukeConf.Project nukeConfProject in SlugCIConfig.Projects)
			{
				if (nukeConfProject.IsTestProject)
				{
					string fullName = TestsDirectory / nukeConfProject.Name / nukeConfProject.Name + ".csproj";
					Project project = Solution.GetProject(fullName);
					ControlFlow.Assert(project != null,
									   "Unable to find the project named: " +
									   nukeConfProject.Name +
									   " in the Nuke Solution.  May need to re-run SlugNuke Setup");
					AbsolutePath CoveragePath = OutputDirectory / ("Coverage_" + project.Name);
					DotNetTest(t => t.SetProjectFile(project.Directory)
									 .SetConfiguration(Configuration)
									 .EnableNoBuild()
									 .EnableNoRestore()
									 .SetProcessLogOutput(true)
									 .SetProcessArgumentConfigurator(arguments => arguments.Add("/p:CollectCoverage={0}", true)
																		 .Add("/p:CoverletOutput={0}/", CoveragePath)
																		 .Add("/p:CoverletOutputFormat={0}", "cobertura")
																		 .Add("/p:Threshold={0}", SlugCIConfig.CodeCoverageThreshold)
																		 .Add("/p:SkipAutoProps={0}", true)
																		 .Add("/p:ExcludeByAttribute={0}",
																			  "\"Obsolete%2cGeneratedCodeAttribute%2cCompilerGeneratedAttribute\"")
																		 .Add("/p:UseSourceLink={0}", true))
									 .SetResultsDirectory(OutputDirectory / "Tests"));

				}
			}
			*/
		}



		public bool CodeCoverage () {
			FileSystemTasks.EnsureExistingDirectory(CoveragePath);
			ReportGeneratorTasks.ReportGenerator(r => r.SetTargetDirectory(CoveragePath)
			                                           .SetProcessWorkingDirectory(CoveragePath)
			                                           .SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
			                                           .SetReports("coverage.cobertura.xml")
			                                           .SetProcessToolPath("reportgenerator"));

			AbsolutePath coverageFile = CoveragePath / "index.html";
			Process.Start(@"cmd.exe ", @"/c " + coverageFile);

			/*
			.DependsOn(Test)
				.Executes(() => {
					if (!SlugCIConfig.UseCodeCoverage) return;

					foreach (NukeConf.Project nukeConfProject in SlugCIConfig.Projects)
					{
						if (nukeConfProject.IsTestProject)
						{
							string fullName = TestsDirectory / nukeConfProject.Name / nukeConfProject.Name + ".csproj";
							Project project = Solution.GetProject(fullName);
							ControlFlow.Assert(project != null,
							                   "Unable to find the project named: " +
							                   nukeConfProject.Name +
							                   " in the Nuke Solution.  May need to re-run SlugNuke Setup");
							AbsolutePath CoveragePath = OutputDirectory / ("Coverage_" + project.Name);

							ReportGenerator(r => r.SetTargetDirectory(CoveragePath)
							                      .SetProcessWorkingDirectory(CoveragePath)
							                      .SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
							                      .SetReports("coverage.cobertura.xml")
							                      .SetProcessToolPath("reportgenerator"));

							AbsolutePath coverageFile = CoveragePath / "index.html";
							Process.Start(@"cmd.exe ", @"/c " + coverageFile);
						}
					}
			*/
			return true;
		}



		public bool Pack () {
			//OutputDirectory.GlobFiles("*.nupkg", "*symbols.nupkg").ForEach(DeleteFile);

			DotNetPackSettings settings = new DotNetPackSettings();
			foreach ( Nuke.Common.ProjectModel.Project x in Solution.AllProjects ) {
				settings.Project = x.Path;
				//settings.SetProject(x.Path);
				settings.OutputDirectory = ArtifactPath;
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



		
		public bool CopyCompiledProject (string source, string destination) {
			FileSystemTasks.CopyDirectoryRecursively(source,destination);

			return true;
		}
	}
}
