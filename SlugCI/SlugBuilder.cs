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

		private ExecutionPlan _executionPlan = new ExecutionPlan();

		private GitProcessor _gitProcessor;

		/// <summary>
		/// Constructor
		/// </summary>
		public SlugBuilder (CISession ciSession) {
			Misc.WriteMainHeader("SlugBuilder:: Startup");

			CISession = ciSession;
			GitProcessorStartup();


			// Setup Build Execution Plan based upon caller's Final Build Request Target
			// Pretend it was compile
			Console.ForegroundColor = ConsoleColor.White;
			LoadBuildStages();

			// TODO Remove or comment this out, this is for speeding up testing.
			foreach ( BuildStage stage in _executionPlan.KnownStages ) {
			//	if ( stage.Name != BuildStageStatic.STAGE_CALCVERSION && stage.Name != BuildStageStatic.STAGE_PUBLISH ) stage.ShouldSkip = true;
			}


			// TODO Need to set this based upon Argument from Program.cs
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
			_executionPlan.AddKnownStage(new BuildStage_CalcVersion(CISession)); 
			_executionPlan.AddKnownStage(new BuildStage_Compile(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Test(CISession));
			_executionPlan.AddKnownStage(new BuildStage_GitCommit(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Cover(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Pack(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Publish(CISession));
		}



		/// <summary>
		/// Initializes the GitProcessor and ensures Repo is in the proper state
		/// </summary>
		private void GitProcessorStartup()
		{
			// Setup the GitProcessor
			_gitProcessor = new GitProcessor(CISession);

			// Get current branch and ensure there are no uncommitted updates.  These methods will throw if anything is out of sorts.
			_gitProcessor.GetCurrentBranch();
			_gitProcessor.RefreshUncommittedChanges();
			_gitProcessor.RefreshLocalBranchStatus();

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
