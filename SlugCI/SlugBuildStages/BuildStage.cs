using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Nuke.Common;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;


namespace Slug.CI
{
	/// <summary>
	/// Provides summary information about a given Stage in the SlugBuilder processing sequence
	/// </summary>
	public abstract class BuildStage  : IEqualityComparer<BuildStage>, IEquatable<BuildStage>
	{
		/// <summary>
		/// The name of the stage
		/// </summary>
		public string Name { get; }
		


		/// The Session information
		protected  CISession CISession { get; }


		/// <summary>
		/// Status of the stage after all processing within it is finished.
		/// </summary>
		public StageCompletionStatusEnum CompletionStatus { get; set; }


		/// <summary>
		/// Determines if the stage should be skipped or not...
		/// </summary>
		public bool ShouldSkip { get; set; }


		/// <summary>
		/// Keeps track of total runtime of this stage.
		/// </summary>
		private Stopwatch _stopwatch;


		/// <summary>
		/// The list of required predecessors
		/// </summary>
		public List<string> PredecessorList { get;} = new List<string>();


		/// <summary>
		/// All recorded output from the stage.  Note: How much is output is determined by the Verbosity setting for the stage.
		/// </summary>
		public List<LineOut> StageOutput { get; private set; } = new List<LineOut>();


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">The name of this stage</param>
		/// <param name="ciSession">The CISession object</param>
		public BuildStage (string name, CISession ciSession) {
			CompletionStatus = StageCompletionStatusEnum.NotStarted;
			Name = name;
			CISession = ciSession;
		}


		/// <summary>
		/// Owner of this stage record, calls this to indicate the Stage has completed all processing.
		/// </summary>
		public void Finished (StageCompletionStatusEnum completionStatus) {
			CompletionStatus = completionStatus;
			_stopwatch.Stop();
		}


		/// <summary>
		/// Returns the runtime of this stage in milliseconds
		/// </summary>
		/// <returns></returns>
		public long RunTimeDuration () {
			if ( _stopwatch == null ) return 0;
			return _stopwatch.ElapsedMilliseconds;
		}



		/// <summary>
		/// Adds a predecessor to this object.
		/// </summary>
		/// <param name="predecessorName"></param>
		public void AddPredecessor (string predecessorName) {
			PredecessorList.Add(predecessorName);
		}


		protected void AddOutputText (string text, OutputType outputType) {
			LineOut output;
			if (outputType == OutputType.Success) output = LineOut.Success(text);
			else if (outputType == OutputType.Err) output = LineOut.Error(text);
			else if (outputType == OutputType.Warn) output = LineOut.Warning(text);
			else if (outputType == OutputType.Info) output = LineOut.Info(text);
			else output = LineOut.Normal(text);
			StageOutput.Add(output);
		}


		/// <summary>
		/// Method that must be overridden in child classes.  This is where the main logic for the child process lives.
		/// </summary>
		/// <returns></returns>
		protected abstract StageCompletionStatusEnum ExecuteProcess ();


		/// <summary>
		/// For stages that have to process each project of a solution, this allows the overall status to be set according to the
		/// lowest status of any given project.  So, if 1 project out of 4 failed, the stage status is failed.
		/// </summary>
		/// <param name="status"></param>
		protected void SetInprocessStageStatus (StageCompletionStatusEnum status) {
			if ( CompletionStatus == StageCompletionStatusEnum.NotStarted ) {
				CompletionStatus = status;
				return;
			}

			if ( CompletionStatus == status ) return;

			// If the current status is warning or error and the new status is good, leave at the current status
			if ( CompletionStatus < StageCompletionStatusEnum.Skipped && status > StageCompletionStatusEnum.Skipped ) return;

			if ( status == StageCompletionStatusEnum.Failure ) {
				CompletionStatus = status;
				return;
			}

			// Warning does not override errors.
			if ( status == StageCompletionStatusEnum.Warning && CompletionStatus <= status ) return;

			// Catchall
			CompletionStatus = status;

		} 


		/// <summary>
		/// Runs the given Build Stage
		/// </summary>
		/// <returns></returns>
		public bool Execute () {
			try {
				// Start stage timer
				_stopwatch = Stopwatch.StartNew();
				
				Console.ForegroundColor = Color.WhiteSmoke;

				Misc.WriteMainHeader("SlugBuilder::  " + Name);


				// Set log level to std.  Let the process override if necessary.
				Logger.LogLevel = LogLevel.Normal;


				if ( ShouldSkip ) {
					Console.WriteLine("Stage skipped due to skip stage setting being set.", Color.Yellow);
					CompletionStatus = StageCompletionStatusEnum.Skipped;
					_stopwatch.Stop();
					return true;
				}
				else
					Finished(ExecuteProcess());


				// Write Finishing Stage Message
				Color lineColor = Color.Red;

				lineColor = CompletionStatus switch
				{
					StageCompletionStatusEnum.Success => Color.Green,
					StageCompletionStatusEnum.Skipped => Color.Cyan,
					StageCompletionStatusEnum.Warning => Color.Yellow,
					_ => Color.Red,
				};

				
				Console.WriteLine("Stage Result:  {0}", CompletionStatus.ToString(), lineColor);


				// Return success / Failure result
				if ( CompletionStatus >= StageCompletionStatusEnum.Warning ) return true;
			}
			catch ( ProcessException p ) {
				CompletionStatus = StageCompletionStatusEnum.Failure;
				Logger.Error(p,true);
			}
			catch ( Exception e ) {
				CompletionStatus = StageCompletionStatusEnum.Failure;
				Logger.Error(e);
			}
			return false;
		}



		bool IEqualityComparer<BuildStage>.Equals(BuildStage x, BuildStage y) {
			if ( x.Name == y.Name ) return true;
			return false;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as BuildStage);
		}

		public bool Equals(BuildStage other)
		{
			return other != null &&
				   Name == other.Name;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name);
		}

		public int GetHashCode([DisallowNull] BuildStage obj) {
			return obj.GetHashCode();
		}

		public static bool operator ==(BuildStage left, BuildStage right)
		{
			return EqualityComparer<BuildStage>.Default.Equals(left, right);
		}

		public static bool operator !=(BuildStage left, BuildStage right)
		{
			return !(left == right);
		}


		/// <summary>
		/// Build Stages display name and Completion Status
		/// </summary>
		/// <returns></returns>
		public override string ToString () { return Name + " [ " + CompletionStatus + " ]"; }
	}
}
