using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Semver;
using Slug.CI.NukeClasses;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Cleaning stage
	/// </summary>
	class BuildStage_CalcVersion : BuildStage {
		private GitProcessor _gitProcessor;

		private List<RecordBranchLatestCommit> latestCommits;
		private Dictionary<string, GitBranchInfo> branches = new Dictionary<string, GitBranchInfo>();
		private string currentBranchName;


		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_CalcVersion (CISession ciSession) : base(BuildStageStatic.STAGE_CALCVERSION, ciSession) { ; }


		/// <summary>
		/// Retrieves information about all the branches (local and remote).  It removes
		/// remote branches that are the exact same as local branches.  At this point in the
		/// process this should result in all remote branches that have local counterparts
		/// being removed from our processing lists.
		/// </summary>
		private void GetBranchInfo () {
			currentBranchName = CISession.GitProcessor.CurrentBranch.ToLower();
			latestCommits = CISession.GitProcessor.GetAllBranchesWithLatestCommit();
			foreach (RecordBranchLatestCommit recordBranchLatestCommit in latestCommits)
			{
				GitBranchInfo branch = new GitBranchInfo(recordBranchLatestCommit, CISession.GitProcessor);
				branches.Add(branch.Name, branch);
			}

			// Process the Dictionary and remove the remotes that are exact same as locals
			List<string> branchesToRemove = new List<string>();
			foreach (KeyValuePair<string, GitBranchInfo> branch in branches)
			{
				if (branch.Value.Name.StartsWith("remotes/origin"))
				{
					string searchName = branch.Value.Name.Substring(15);
					if (branches.ContainsKey(searchName))
					{
						GitBranchInfo b = branches[searchName];
						if (branch.Value.IsSameAs(b, CISession.VerbosityCalcVersion)) branchesToRemove.Add(branch.Value.Name);
					}
				}
			}

			foreach (string s in branchesToRemove) { branches.Remove(s); }
		}


		/// <summary>
		/// Runs the Calculate Version process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess () {
			SemVersion semVersion;

			GetBranchInfo();

			GitBranchInfo comparisonBranch = null;


			string branchToFind = "";
			if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) branchToFind = "alpha";
			if ( CISession.PublishTarget == PublishTargetEnum.Beta ) branchToFind = "beta";

			//if ( CISession.PublishTarget == PublishTargetEnum.Production ) throw new NotImplementedException();

			// The branch does not exist, user must create it.
			if ( !branches.TryGetValue(branchToFind, out comparisonBranch) )
				ControlFlow.Assert(true == false, "The destination branch [" + branchToFind + "] does not exist.  You must manually create this branch.");

			// Get Main Branch info
			GitBranchInfo mainBranch = branches [CISession.GitProcessor.MainBranchName];


			// Determine the branch we are coming from
			//string branchLC = CISession.GitProcessor.CurrentBranch.ToLower();
			bool incrementPatch = (currentBranchName.StartsWith("fix") || currentBranchName.StartsWith("bug"));
			bool incrementMinor = (currentBranchName.StartsWith("feature") || currentBranchName.StartsWith("feat"));

			string destBranchType = "alpha";
			if ( incrementMinor ) destBranchType = "beta";

			SemVersion newVersion = null;
			// If comparison is null then we are deploying to main
			if ( CISession.PublishTarget== PublishTargetEnum.Production) {
				throw new NotImplementedException();
			}
			else {
				// Get most recent Version Tag for the desired branch type
				SemVersion mostRecentBranchTypeVersion = CISession.GitProcessor.GetMostRecentVersionTagOfBranch(destBranchType);

				// See if main is newer and if it's tag is newer.  If so we need to set the comparison's
				// tag to the main version and then add the alpha / beta to it...
				if ( mainBranch.LatestSemVersionOnBranch > mostRecentBranchTypeVersion ) {
					newVersion = new SemVersion(mainBranch.LatestSemVersionOnBranch.Major, mainBranch.LatestSemVersionOnBranch.Minor,
					                            mainBranch.LatestSemVersionOnBranch.Patch, comparisonBranch.Name + ".0001");
					
				}
				else {
					string pre = mostRecentBranchTypeVersion.Prerelease;
					int index = pre.IndexOf('.');
					if (index == -1 ) ControlFlow.Assert(true == false, "Unable to find the . in the prerelease portion of the semanticVersion [" + comparisonBranch.LatestSemVersionOnBranch.Prerelease + "]");
					bool isInt = int.TryParse(pre.Substring(++index), out int value);
					ControlFlow.Assert(isInt, "PreRelease tag of [" + comparisonBranch.LatestSemVersionOnBranch.Prerelease + "] does not contain an integer component after the .");
					value++;
					pre = comparisonBranch.Name + "." + value.ToString("D4");
					newVersion = new SemVersion(mostRecentBranchTypeVersion.Major, mostRecentBranchTypeVersion.Minor,
					                            mostRecentBranchTypeVersion.Patch, pre);
				}
			}

			// Store the version that should be set for the build.
			CISession.SemVersion = newVersion;
			
			return StageCompletionStatusEnum.Success;
		}


		private void DeployProduction () {

		}
	}
}
