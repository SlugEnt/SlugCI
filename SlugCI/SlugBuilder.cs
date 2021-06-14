using Nuke.Common;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Nuke.Common.Tooling;
using Semver;
using Console = Colorful.Console;

namespace Slug.CI
{
	/// <summary>
	/// Responsible for the actual building and running of the build process for the solution.
	/// </summary>
	public class SlugBuilder {
		private ExecutionPlan _executionPlan = new ExecutionPlan();
		private GitProcessor _gitProcessor;


		/// <summary>
		/// The session information
		/// </summary>
		private CISession CISession { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public SlugBuilder (CISession ciSession) {
			Misc.WriteMainHeader("SlugBuilder:: Startup");

			CISession = ciSession;
			GitProcessorStartup();


			// Setup Build Execution Plan based upon caller's Final Build Request Target
			// Pretend it was compile
			Console.ForegroundColor = Color.WhiteSmoke;
			LoadBuildStages();


			// TODO Remove or comment this out, this is for speeding up testing.
#if DEBUG
			/*
			foreach ( BuildStage stage in _executionPlan.KnownStages ) {
				//if ( stage.Name != BuildStageStatic.STAGE_TYPEWRITER_PUBLISH && stage.Name != BuildStageStatic.STAGE_TYPEWRITER_VER) stage.ShouldSkip = true;
//				if ( stage.Name != BuildStageStatic.STAGE_PUBLISH ) stage.ShouldSkip = true;
				if ( stage.Name != BuildStageStatic.STAGE_TYPEWRITER_PUBLISH  && 
				     stage.Name != BuildStageStatic.STAGE_TYPEWRITER_VER &&
				     stage.Name != BuildStageStatic.STAGE_PUBLISH) stage.ShouldSkip = true;
			}
/			// Only need this if we have skipped the calc version step above...
			ciSession.VersionInfo = new VersionInfo(new SemVersion(3,56,43),"656gtg" );
*/
#endif

			_executionPlan.BuildExecutionPlan(BuildStageStatic.STAGE_FINAL);


			// Anything less than skipped indicates an error situation.
			StageCompletionStatusEnum planStatus = _executionPlan.Execute();

			WriteSummary(_executionPlan, CISession.IsInteractiveRun, ciSession);


			// TODO Move this somewhere...
//			BuildStage_TypeWriterPublish tw = (BuildStage_TypeWriterPublish) _executionPlan.GetBuildStage(BuildStageStatic.STAGE_TYPEWRITER_PUBLISH);
//			foreach ( LineOut output in tw.StageOutput ) { Console.WriteLine(output); }
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
			_executionPlan.AddKnownStage(new BuildStage_TypeWriterVersioning(CISession));
			_executionPlan.AddKnownStage(new BuildStage_GitCommit(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Cover(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Pack(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Publish(CISession));
			_executionPlan.AddKnownStage(new BuildStage_TypeWriterPublish(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Final(CISession));
			_executionPlan.AddKnownStage(new BuildStage_Angular(CISession));

		}



		/// <summary>
		/// Initializes the GitProcessor and ensures Repo is in the proper state
		/// </summary>
		private void GitProcessorStartup () {
			// Setup the GitProcessor
			_gitProcessor = CISession.GitProcessor;

			if ( _gitProcessor.IsCurrentBranchMainBranch() && CISession.PublishTarget != PublishTargetEnum.Production ) {
				string msg =
					@"The current branch is the main branch, yet you are running a Test Publish command.  This is unsupported as it will cause version issues in Git.  " +
					"Either create a branch off master to put the changes into (this is probably what you want) OR change Target command to PublishProd.";
				ControlFlow.Assert(1 == 0, msg);
			}
		}



		/// <summary>
		/// Writes End of slugBuilder Summary info.
		/// </summary>
		internal virtual void WriteSummary (ExecutionPlan plan, bool isInteractive, CISession ciSession) {
			Console.WriteLine();
			Misc.WriteFinalHeader(plan.PlanStatus, ciSession);

			// Build dictionary of each Build Stage in order, assigning a letter to each stage.  The letter will allow user to see detailed info 
			// about the stage. 
			Dictionary<char, BuildStage> stageInfo = new Dictionary<char, BuildStage>();
			char letter = 'A';

			foreach ( BuildStage buildStage in plan.Plan ) {
				stageInfo.Add(letter, buildStage);
				letter++;
			}


			bool continueLooping = true;
			while ( continueLooping ) {

				// Print info for non interactive sessions
				if ( !isInteractive ) {
					if ( Logger.OutputSink.SevereMessages.Count > 0 ) {
						CISession.OutputSink.WriteNormal("");
						WriteSevereMessages();
						WriteStageStats(plan, stageInfo);
					}
				}
				else {
					CISession.OutputSink.WriteNormal("");
					WriteStageStats(plan, stageInfo);
					CISession.OutputSink.WriteNormal("");
				}


				// Write Version info if successful build
				if ( plan.WasSuccessful ) {
					CISession.OutputSink.WriteSuccess($"Build succeeded on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
					Console.WriteLine();
					Console.WriteLine("Version Built was: ", Color.Yellow);
					Console.WriteLine("    Semantic Version:   " + ciSession.VersionInfo.SemVersionAsString, Color.Yellow);
					Console.WriteLine("    Assembly Version:   " + ciSession.VersionInfo.AssemblyVersion, Color.Yellow);
					Console.WriteLine("    File Version:       " + ciSession.VersionInfo.FileVersion, Color.Yellow);
					Console.WriteLine("    Info Version:       " + ciSession.VersionInfo.InformationalVersion, Color.Yellow);
				}
				else
					CISession.OutputSink.WriteError($"Build failed on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");


				// Exit if non-interactive as rest is user prompting and responding
				if ( !isInteractive ) return;

				Console.WriteLine("Press (x) to exit, (1) to display git history  (9) to view detailed error information  OR");
				Console.WriteLine("  Press letter from above build stage to see detailed output of that build stage.");
				ConsoleKeyInfo keyInfo = Console.ReadKey();
				if ( keyInfo.Key == ConsoleKey.X ) return;

				// Detailed error info
				if ( keyInfo.Key == ConsoleKey.D9 ) {
					CISession.OutputSink.WriteNormal("");
					WriteSevereMessages();
				}

				// Git history
				if ( keyInfo.Key == ConsoleKey.D1 ) {
					ciSession.GitProcessor.PrintGitHistory();
					Console.WriteLine("Press [space] key to return to menu", Color.Yellow);
					while (Console.ReadKey().Key != ConsoleKey.Spacebar) { }
				}

				// Check to see if letter is in StageInfo Dictionary.
				char keyPress = keyInfo.KeyChar;
				if ( keyPress > 96 ) keyPress = (char) (keyPress - 32);


				// Display detailed info about a specific stage...
				if ( stageInfo.ContainsKey(keyPress) ) {
					// Display detailed info
					BuildStage stage = stageInfo [keyPress];

					Console.WriteLine();
					Misc.WriteSubHeader(stage.Name, new List<string>() {"Detailed Output"});
					Color lineColor = Color.WhiteSmoke;
					foreach ( LineOut output in stage.StageOutput ) {
						output.WriteToConsole();
					}

					Console.WriteLine();
					Console.WriteLine("Press [space] key to return to menu", Color.Yellow);
					while ( Console.ReadKey().Key != ConsoleKey.Spacebar ) { }
				}
			}
		}



		// FX Builds the duration from the Runtime
		static string GetDuration(long duration)
		{
			long seconds = duration / 1000;
			if (seconds < 1)
				return "< 1 second";
			else
				return seconds.ToString();
		}



		/// <summary>
		/// Writes the completion stats for the given build stages.
		/// </summary>
		protected virtual void WriteStageStats(ExecutionPlan plan, Dictionary<char, BuildStage> stageInfo)
		{
			
			int firstColumn = 8;
			int secondColumn = Math.Max(stageInfo.Max(x => x.Value.Name.Length) + 4, 20); 
			int thirdColumn = 10;
			int fourthColumn = 10;
			int totalColWidth = firstColumn + secondColumn + thirdColumn + fourthColumn;
			

			// FX Creates a writable line
			string CreateLine(char letter, string name, string status, string duration) =>
				 "( " + letter + " )  " + 
				name.PadRight(secondColumn, paddingChar: ' ') +
				status.PadRight(thirdColumn, paddingChar: ' ') +
				duration.PadLeft(fourthColumn, paddingChar: ' ');


			// FX builds duration value
			static string GetDurationOrBlank(BuildStage target)
				=> target.CompletionStatus == StageCompletionStatusEnum.Success ||
					target.CompletionStatus == StageCompletionStatusEnum.Failure ||
					target.CompletionStatus == StageCompletionStatusEnum.Warning ||
					target.CompletionStatus == StageCompletionStatusEnum.InProcess ||
					target.CompletionStatus == StageCompletionStatusEnum.Aborted
					? GetDuration(target.RunTimeDuration())
					: string.Empty;


			// Begin writing table
			CISession.OutputSink.WriteNormal(new string(c: '═', count: totalColWidth));
			CISession.OutputSink.WriteInformation(CreateLine(' ',"Target", "Status", "Duration"));
			CISession.OutputSink.WriteNormal(new string(c: '─', count: totalColWidth));

			long totalDuration = 0;
			foreach (KeyValuePair<char,BuildStage> entry in stageInfo) {
				BuildStage stageStat = entry.Value;
				string line = CreateLine(entry.Key,stageStat.Name, stageStat.CompletionStatus.ToString(), GetDurationOrBlank(stageStat));
				totalDuration += stageStat.RunTimeDuration();

				switch (stageStat.CompletionStatus)
				{
					case StageCompletionStatusEnum.Skipped:
						CISession.OutputSink.WriteNormal(line);
						break;
					case StageCompletionStatusEnum.Success:
						CISession.OutputSink.WriteSuccess(line);
						break;
					case StageCompletionStatusEnum.Aborted:
						CISession.OutputSink.WriteWarning(line);
						break;
					case StageCompletionStatusEnum.Failure:
						CISession.OutputSink.WriteError(line);
						break;
					case StageCompletionStatusEnum.Warning:
						CISession.OutputSink.WriteWarning(line);
						break;
					case StageCompletionStatusEnum.NotStarted:
						CISession.OutputSink.WriteWarning(line);
						break;
					default:
						throw new NotSupportedException(stageStat.CompletionStatus.ToString());
				}
			}


			CISession.OutputSink.WriteNormal(new string(c: '─', count: totalColWidth));
			CISession.OutputSink.WriteInformation(CreateLine(' ',"Total", string.Empty, GetDuration(totalDuration)));
			CISession.OutputSink.WriteNormal(new string(c: '═', count: totalColWidth));
		}



		/// <summary>
		/// Writes out all the errors and warnings encountered during program run.
		/// </summary>
		protected virtual void WriteSevereMessages()
		{
			CISession.OutputSink.WriteInformation("Repeating warnings and errors:");

			foreach (OutputRecord record in CISession.OutputSink.SevereMessages)
			{
				switch (record.LogLevel)
				{
					case LogLevel.Warning:
						CISession.OutputSink.WriteWarning(record.Text, record.Details);
						break;
					case LogLevel.Error:
						CISession.OutputSink.WriteError(record.Text, record.Details);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}
