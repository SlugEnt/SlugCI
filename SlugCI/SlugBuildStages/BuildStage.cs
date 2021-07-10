using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using CmdProcessor;
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
		public List<ILineOut> StageOutput { get; private set; } = new List<ILineOut>();


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

				Misc.WriteMainHeader("ConvertToSlugCI::  " + Name);


				// Set log level to std.  Let the process override if necessary.
				Logger.LogLevel = LogLevel.Normal;


				if ( ShouldSkip ) {
					AOT_Warning("Stage skipped due to skip stage setting being set.");
					CompletionStatus = StageCompletionStatusEnum.Skipped;
					_stopwatch.Stop();
					return true;
				}

				CompletionStatus = StageCompletionStatusEnum.InProcess;
				Finished(ExecuteProcess());


				string finalMsg = String.Format("Stage Result:  {0}", CompletionStatus.ToString());

				Color lineColor = CompletionStatus switch
				{
					StageCompletionStatusEnum.Success => Color.Green,
					StageCompletionStatusEnum.Skipped => Color.Cyan,
					StageCompletionStatusEnum.Warning => Color.Yellow,
					_ => Color.Red,
				};
				AOT_Normal(finalMsg,lineColor);


				// Return success / Failure result
				if ( CompletionStatus >= StageCompletionStatusEnum.Warning ) return true;
			}
			catch ( ProcessException p ) {
				CompletionStatus = StageCompletionStatusEnum.Failure;
				AOT_Error(p);
				//Logger.Error(p,true);
			}
			catch ( Exception e ) {
				CompletionStatus = StageCompletionStatusEnum.Failure;
				AOT_Error(e);
				//Logger.Error(e);
			}
			return false;
		}


#region "Equality Methods"
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

#endregion


		/// <summary>
		/// If True, all the AddOutputText functions will store the given output to the StageOutput list AND write it to console.
		/// If False, it only logs it to the StageOutput List.
		/// </summary>
		public bool ShouldLogToConsoleRealTime { get; set; } = true;


		/// <summary>
		/// Adds the given text to output list and logs it to screen.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="outputType"></param>
		public void AddOutputText(string text, OutputType outputType)
		{
			LineOut output;
			if (outputType == OutputType.Success) AOT_Success(text);
			else if (outputType == OutputType.Err) AOT_Error(text);
			else if (outputType == OutputType.Warn) AOT_Warning(text);
			else if (outputType == OutputType.Info) AOT_Info(text);
			else AOT_Normal(text);
		}


		/// <summary>
		/// Writes the given text as Success output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		public void AOT_Success (string text) {
			StageOutput.Add(LineOutColored.Success(text));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text,Color.Green);
		}


		/// <summary>
		/// Writes the given text as Errored output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Error(Exception exception) {
			string exceptionSeparator = "************************  [ Exception Encountered ] ************************";
			int start = StageOutput.Count;
			StageOutput.Add(LineOutColored.Error(exceptionSeparator));
			StageOutput.Add(LineOutColored.Error(exception.Message));
			StageOutput.Add(LineOutColored.Error(exceptionSeparator));
			StageOutput.Add(LineOutColored.NewLine());
			StageOutput.Add(LineOutColored.Error(exception.ToString()));
			StageOutput.Add(LineOutColored.NewLine());

			
			if (ShouldLogToConsoleRealTime) Print_StageOutput(start);
		}



		/// <summary>
		/// Writes the given text as Errored output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Error (string text)
		{
			StageOutput.Add(LineOutColored.Error(text));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text,Color.Red);
		}


		/// <summary>
		/// Writes the given text as warning output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Warning(string text)
		{
			StageOutput.Add(LineOutColored.Warning(text));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text, Color.Yellow);
		}


		/// <summary>
		/// Writes the given text as Informational output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Info(string text)
		{
			StageOutput.Add(LineOutColored.Info(text));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text, Color.Cyan);
		}


		/// <summary>
		/// Writes the given text as normal output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Normal(string text)
		{
			StageOutput.Add(LineOutColored.Normal(text));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text, Color.WhiteSmoke);
		}


		/// <summary>
		/// Writes the given text as normal output to both BuildStage output and to the console real time.
		/// </summary>
		/// <param name="text"></param>
		protected void AOT_Normal(string text, Color textColor)
		{
			StageOutput.Add(LineOutColored.Normal(text,textColor));
			if (ShouldLogToConsoleRealTime) Console.WriteLine(text, textColor);
		}


		/// <summary>
		/// Writes a new blank line
		/// </summary>
		protected void AOT_NewLine () {
			StageOutput.Add(LineOutColored.NewLine());
			if (ShouldLogToConsoleRealTime) Console.WriteLine();
		}


		/// <summary>
		/// Prints the given lines of StageOutput
		/// </summary>
		/// <param name="startLine">Starting index number of StageOutput to print.  Is zero based.</param>
		/// <param name="endLine">Ending index number of StageOutput to print.  Is 1 based.  If value is zero then it prints to end of list</param>
		public void Print_StageOutput (int startLine, int endLine = 0) {
			if ( endLine == 0 ) endLine = StageOutput.Count;
			if ( StageOutput.Count == 0 ) return;

			int count = endLine - startLine;
			if ( startLine > StageOutput.Count ) return;
			for (int i = startLine; i < endLine; i++) 
				StageOutput[i].WriteToConsole();
				//Console.WriteLine(StageOutput[i].Text,StageOutput[i].FGColor);
		}
	}
}
