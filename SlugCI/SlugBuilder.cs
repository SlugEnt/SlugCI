using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
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


		private ExecutionPlan _executionPlan = new ExecutionPlan();

		private GitProcessor _gitProcessor;

		/// <summary>
		/// Constructor
		/// </summary>
		public SlugBuilder (CISession ciSession) {
			Misc.WriteMainHeader("SlugBuilder:: Startup");

			CISession = ciSession;


			// Temporary code

				// TODO Fix this code

				GitRepository = GitRepository.FromLocalDirectory(@"C:\A_Dev\SlugEnt\NukeTestControl\");

			// Set Path properties


			// Load All Known Build Stages

			// TODO - Uncomment
			//GitProcessorStartup();


			// Setup Build Execution Plan based upon caller's Final Build Request Target
			// Pretend it was compile
			Console.ForegroundColor = ConsoleColor.White;
			LoadBuildStages();
			_executionPlan.BuildExecutionPlan(BuildStageStatic.STAGE_PUBLISH);


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
			_executionPlan.AddKnownStage(new BuildStage_Test(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Pack(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Cover(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Publish(CISession));
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
