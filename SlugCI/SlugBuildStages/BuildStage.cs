using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Build.Framework;
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
		private  CISession CISession { get; }


		/// <summary>
		/// Status of the stage after all processing within it is finished.
		/// </summary>
		public StageCompletionStatusEnum CompletionStatus { get; set; }



		private Stopwatch _stopwatch;


		/// <summary>
		/// The list of required predecessors
		/// </summary>
		public List<string> PredecessorList { get;} = new List<string>();


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">The name of this stage</param>
		/// <param name="ciSession">The CISession object</param>
		public BuildStage (string name, CISession ciSession) {
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
			return _stopwatch.ElapsedMilliseconds;
		}



		/// <summary>
		/// Adds a predecessor to this object.
		/// </summary>
		/// <param name="predecessorName"></param>
		public void AddPredecessor (string predecessorName) {
			PredecessorList.Add(predecessorName);
		}


		public bool Execute () {
			// Start stage timer
			_stopwatch = Stopwatch.StartNew();


			Finished(StageCompletionStatusEnum.Success);
			return true;
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


		public override string ToString () { return Name; }
	}
}
