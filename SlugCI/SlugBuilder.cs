using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.OutputSinks;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;

namespace Slug.CI
{
	public class SlugBuilder
	{
		/// <summary>
		/// The session information
		/// </summary>
		private CISession CISession { get; set; }

		/// <summary>
		/// The Git Repository for the Solution
		/// </summary>
		public GitRepository GitRepository { get; set; }


		/// <summary>
		/// The temporary storage path
		/// </summary>
		public AbsolutePath ArtifactPath { get;private set; }
		

		/// <summary>
		/// Where coverage reports are located
		/// </summary>
		public AbsolutePath CoveragePath { get; private set; }


		/// <summary>
		/// Test output location
		/// </summary>
		public AbsolutePath TestOutputPath { get; private set; }

		private ExecutionPlan _executionPlan = new ExecutionPlan();

		private GitProcessor _gitProcessor;

		/// <summary>
		/// Constructor
		/// </summary>
		public SlugBuilder (CISession ciSession) {
			Misc.WriteMainHeader("SlugBuilder:: Startup");

			CISession = ciSession;



			// TODO Fix this code

			GitRepository = GitRepository.FromLocalDirectory(@"C:\A_Dev\SlugEnt\NukeTestControl\");

			// Set Path properties
			
			CoveragePath = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\artifacts\Coverage";
			TestOutputPath = (AbsolutePath)@"C:\A_Dev\SlugEnt\NukeTestControl\artifacts\Tests";


			// Load All Known Build Stages

			// TODO - Uncomment
			//GitProcessorStartup();


			// Setup Build Execution Plan based upon caller's Final Build Request Target
			// Pretend it was compile
			LoadBuildStages();
			_executionPlan.BuildExecutionPlan(BuildStageStatic.STAGE_COMPILE);


			// Anything less than skipped indicates an error situation.
			StageCompletionStatusEnum planStatus = _executionPlan.Execute();

			Logger.OutputSink.WriteSummary(_executionPlan, CISession.IsInteractiveRun);

		}


		/// <summary>
		/// Adds all the known Build stages to a list. 
		/// </summary>
		private void LoadBuildStages () {
			_executionPlan.AddKnownStage(new BuildStage_Clean(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Restore(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Compile(CISession));
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

		public bool Test () {
			Misc.WriteMainHeader("SlugBuilder:: Run Unit Tests");
			FileSystemTasks.EnsureExistingDirectory(CoveragePath);

			DotNetTestSettings settings = new DotNetTestSettings()
			{
				ProjectFile = CISession.Solution,
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
			Misc.WriteMainHeader("SlugBuilder:: CodeCoverage");
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
			Misc.WriteMainHeader("SlugBuilder::  Nuget Pack");
			//OutputDirectory.GlobFiles("*.nupkg", "*symbols.nupkg").ForEach(DeleteFile);

			DotNetPackSettings settings = new DotNetPackSettings();
			foreach ( Nuke.Common.ProjectModel.Project x in CISession.Solution.AllProjects ) {
				settings.Project = x.Path;
				//settings.SetProject(x.Path);
				settings.OutputDirectory = ArtifactPath;
				settings.IncludeSymbols = true;
				settings.NoRestore = true;
				settings.Verbosity = DotNetVerbosity.Diagnostic;
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
			Misc.WriteMainHeader("SlugBuilder:: Deploy Via Copy");
			FileSystemTasks.CopyDirectoryRecursively(source,destination);

			return true;
		}


		/// <summary>
		/// Initializes the GitProcessor and ensures Repo is in the proper state
		/// </summary>
		private void GitProcessorStartup()
		{
			// Setup the GitProcessor
			_gitProcessor = new GitProcessor(CISession);
			if (_gitProcessor.GitVersion == null) Logger.Error("GitProcessor:  Unable to load the GitVersion not Loaded");

			// Get current branch and ensure there are no uncommitted updates.  These methods will throw if anything is out of sorts.
			_gitProcessor.GetCurrentBranch();
			_gitProcessor.IsUncommittedChanges();
			_gitProcessor.IsBranchUpToDate();

			if (_gitProcessor.IsCurrentBranchMainBranch() && CISession.PublishTarget != PublishTargetEnum.Production)
			{
				string msg =
					@"The current branch is the main branch, yet you are running a Test Publish command.  This is unsupported as it will cause version issues in Git.  " +
					"Either create a branch off master to put the changes into (this is probably what you want) OR change Target command to PublishProd.";
				ControlFlow.Assert(1 == 0, msg);
			}
		}

	}
}
