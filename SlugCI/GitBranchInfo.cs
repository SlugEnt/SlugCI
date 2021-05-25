using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Semver;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;

namespace Slug.CI
{
	/// <summary>
	/// Provides summarized information about a branch, its most recent tags, commits, etc.
	/// </summary>
	public class GitBranchInfo
	{
		/// <summary>
		/// The branch name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The last Ver tag that this branch was tagged with
		/// </summary>
		public SemVersion LatestSemVersionOnBranch { get; set; } = null;


		/// <summary>
		/// The most recent commit on this branch
		/// </summary>
		public GitCommitInfo LatestCommitOnBranch { get; set; } = null;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="branchName">Name of this branch</param>
		public GitBranchInfo (RecordBranchLatestCommit branchRecord, GitProcessor gitProcessor) {
			Name = branchRecord.branch;

			// Retrieve info about the latest commit on the branch
			LatestCommitOnBranch = gitProcessor.GetCommitInfo(branchRecord.commitHash);
			LatestSemVersionOnBranch = gitProcessor.FindLatestSemVersionOnBranch(Name);
		}


		public override string ToString () {
			return Name + ": " + LatestSemVersionOnBranch + "  [ " + LatestCommitOnBranch.DateCommitted + " ]";
		}


		/// <summary>
		/// Compares 2 GitBranchInfo objects to see if they are the same.  This occurs when there
		/// is a local copy and a remote of the same branch.  They have different names, but everything
		/// else is the same.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public bool IsSameAs (GitBranchInfo b, Verbosity verbosity) {
		if ( LatestCommitOnBranch == b.LatestCommitOnBranch &&
			     LatestSemVersionOnBranch == b.LatestSemVersionOnBranch) return true;
			return false;
		}


		private Color Compare (object a, object b) {
			if ( a.Equals(b) )
				return Color.Green;
			else
				return Color.Yellow;
		}
	}
}
