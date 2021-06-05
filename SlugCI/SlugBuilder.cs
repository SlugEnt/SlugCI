using Nuke.Common;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;
using System;
using Nuke.Common.Tooling;

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
				if ( stage.Name != BuildStageStatic.STAGE_TYPEWRITER && stage.Name != BuildStageStatic.STAGE_TYPEWRITER_VER) stage.ShouldSkip = true;
			}
			_executionPlan.BuildExecutionPlan(BuildStageStatic.STAGE_TYPEWRITER);


			// TODO Need to set this based upon Argument from Program.cs

			_executionPlan.BuildExecutionPlan(BuildStageStatic.STAGE_PUBLISH);


			// Anything less than skipped indicates an error situation.
			StageCompletionStatusEnum planStatus = _executionPlan.Execute();

			Logger.OutputSink.WriteSummary(_executionPlan, CISession.IsInteractiveRun,ciSession);


			// TODO Move this somewhere...
			BuildStage_TypeWriterRun tw =  (BuildStage_TypeWriterRun )_executionPlan.GetBuildStage(BuildStageStatic.STAGE_TYPEWRITER);
			foreach ( Output output in tw.StageOutput ) {
				Console.WriteLine(output);
			}
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
			_executionPlan.AddKnownStage(new BuildStage_TypeWriterRun(CISession));
			_executionPlan.AddKnownStage(new BuildStage_TypeWriterVersioning(CISession));
		}



		/// <summary>
		/// Initializes the GitProcessor and ensures Repo is in the proper state
		/// </summary>
		private void GitProcessorStartup()
		{
			// Setup the GitProcessor
			_gitProcessor = CISession.GitProcessor;

			// Get current branch and ensure there are no uncommitted updates.  These methods will throw if anything is out of sorts.
			// TODO Remove these - they are part of GitProcesor
/*			_gitProcessor.GetCurrentBranch();
			_gitProcessor.RefreshUncommittedChanges();
			_gitProcessor.RefreshLocalBranchStatus();
*/
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
