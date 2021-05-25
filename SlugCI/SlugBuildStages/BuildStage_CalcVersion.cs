using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
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
		private GitBranchInfo mainBranch;

		private bool incrementMinor;
		private bool incrementPatch;
		private bool incrementMajor;


		private SemVersion newVersion = null;
		private SemVersion mostRecentBranchTypeVersion;

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

			if ( CISession.PublishTarget == PublishTargetEnum.Production ) branchToFind = CISession.GitProcessor.MainBranchName;

			// Make sure branch exists.
			if ( !branches.TryGetValue(branchToFind, out comparisonBranch) )
				ControlFlow.Assert(true == false, "The destination branch [" + branchToFind + "] does not exist.  You must manually create this branch.");

			// Get Main Branch info
			mainBranch = branches [CISession.GitProcessor.MainBranchName];


			// Determine the potential version bump...
			incrementPatch = (currentBranchName.StartsWith("fix") || currentBranchName.StartsWith("bug"));
			incrementMinor = (currentBranchName.StartsWith("feature") || currentBranchName.StartsWith("feat"));
			incrementMajor = (currentBranchName.StartsWith("major"));

			string versionPreReleaseName = "alpha";
			if ( CISession.PublishTarget == PublishTargetEnum.Beta ) versionPreReleaseName = "beta";
			
			// Get most recent Version Tag for the desired branch type
			mostRecentBranchTypeVersion = CISession.GitProcessor.GetMostRecentVersionTagOfBranch(versionPreReleaseName);


			if ( CISession.PublishTarget== PublishTargetEnum.Production) {
				CalculateMainVersion();
			}
			else if (CISession.PublishTarget == PublishTargetEnum.Alpha) {
				CalculateAlphaVersion(comparisonBranch);
			}
			else if ( CISession.PublishTarget == PublishTargetEnum.Beta ) {
				CalculateBetaVersion(comparisonBranch);
			}
			else
				throw new ApplicationException("Publish Target of [" + CISession.PublishTarget.ToString() + "]  has no implemented functionality");

			// Store the version that should be set for the build.
			CISession.SemVersion = newVersion;
			
			return StageCompletionStatusEnum.Success;
		}


		private void CalculateBetaVersion (GitBranchInfo comparisonBranch) {
			SemVersion tempVersion;

			// Find the parents max version number.

			GitCommitInfo commitBeta = comparisonBranch.LatestCommitOnBranch;

			// Look for the highest version number from one of its parents and keep it.
			SemVersion newestVersion = new SemVersion(0, 0, 0);
			foreach ( string commitHash in commitBeta.ParentCommits ) {
				GitCommitInfo commitInfo = CISession.GitProcessor.GetCommitInfo(commitHash);
				SemVersion parentVersion = commitInfo.GetGreatestVersionTag();
				if ( parentVersion > newestVersion ) {
					newestVersion = parentVersion;
				}
			}
			SemVersionPreRelease updatedBeta;

			// Build New PreRelease tag based upon old, but with new number and type set to Beta.
			if ( mostRecentBranchTypeVersion >= newestVersion ) {
				newestVersion = mostRecentBranchTypeVersion;
				updatedBeta = new SemVersionPreRelease(newestVersion.Prerelease);
			}
			else {
				// Get pre-release of the newest version, which is not a beta branch
				SemVersionPreRelease preOld = new SemVersionPreRelease(newestVersion.Prerelease);

				// Get pre-release of most current beta branch version
				SemVersionPreRelease currentBeta = new SemVersionPreRelease(mostRecentBranchTypeVersion.Prerelease);

				// Update the beta to be of the Increment Type
				if ( currentBeta.IncrementType < preOld.IncrementType )
					updatedBeta = new SemVersionPreRelease(currentBeta.ReleaseType, currentBeta.ReleaseNumber, preOld.IncrementType);
				else {
					updatedBeta = currentBeta;
				}
				newestVersion = new SemVersion(newestVersion.Major, newestVersion.Minor, newestVersion.Patch, updatedBeta.Tag());
			}


			// Increase version
			if (incrementMinor) updatedBeta.BumpMinor();
			else if (incrementPatch) updatedBeta.BumpPatch();
			else if (incrementMajor) updatedBeta.BumpMajor();
			else updatedBeta.BumpVersion();

			newVersion = new SemVersion(newestVersion.Major, newestVersion.Minor, newestVersion.Patch, updatedBeta.Tag());
		}


		/// <summary>
		/// Computes the version for the Alpha Branch.
		/// </summary>
		/// <param name="comparisonBranch"></para	m>
		private void CalculateAlphaVersion (GitBranchInfo comparisonBranch) {
			// See if main is newer and if it's tag is newer.  If so we need to set the comparison's
			// tag to the main version and then add the alpha / beta to it...
			SemVersion tempVersion = CompareToMainVersion(mostRecentBranchTypeVersion, comparisonBranch.Name);

			SemVersionPreRelease semPre = new SemVersionPreRelease(tempVersion.Prerelease);
			if (incrementMinor) semPre.BumpMinor();
			else if (incrementPatch) semPre.BumpPatch();
			else if (incrementMajor) semPre.BumpMajor();
			else semPre.BumpVersion();

			newVersion = new SemVersion(tempVersion.Major, tempVersion.Minor, tempVersion.Patch, semPre.Tag());
		}


		SemVersion CompareToMainVersion (SemVersion comparisonVersion,string comparisonBranchName) {
			SemVersion tempVersion;

			if (mainBranch.LatestSemVersionOnBranch > comparisonVersion)
			{
				int major = mainBranch.LatestSemVersionOnBranch.Major;
				int minor = mainBranch.LatestSemVersionOnBranch.Minor;
				int patch = mainBranch.LatestSemVersionOnBranch.Patch;
				if (incrementMajor)
				{
					major++;
					minor = 0;
					patch = 0;
				}
				if (incrementMinor)
				{
					minor++;
					patch = 0;
				}
				if (incrementPatch) patch++;

				tempVersion = new SemVersion(major, minor, patch, comparisonBranchName + ".0000");
			}
			else
				tempVersion = new SemVersion(comparisonVersion.Major, comparisonVersion.Minor, comparisonVersion.Patch, comparisonVersion.Prerelease);

			return tempVersion;
		}

		/// <summary>
		/// Calculations to compute the new version when it's a production push.
		/// </summary>
		private void CalculateMainVersion () {
			// Strip out the prerelease portion to determine if main is newer or not
			SemVersion tempVersion =
				new SemVersion(mostRecentBranchTypeVersion.Major, mostRecentBranchTypeVersion.Minor, mostRecentBranchTypeVersion.Patch);
			if (mainBranch.LatestSemVersionOnBranch < tempVersion) newVersion = new SemVersion(tempVersion.Major, tempVersion.Minor, tempVersion.Patch);
			else
			{
				int major = mainBranch.LatestSemVersionOnBranch.Major;
				int minor = mainBranch.LatestSemVersionOnBranch.Minor;
				int patch = mainBranch.LatestSemVersionOnBranch.Patch;
				if (incrementMinor)
				{
					minor++;
					patch = 0;
				}

				if (incrementPatch) patch++;
				newVersion = new SemVersion(major, minor, patch);
			}
		}
	

	}
}
