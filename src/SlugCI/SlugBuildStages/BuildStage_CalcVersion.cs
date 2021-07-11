using Nuke.Common;
using Semver;
using Slug.CI.NukeClasses;
using System;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Cleaning stage
	/// </summary>
	class BuildStage_CalcVersion : BuildStage {
		
		//private Dictionary<string, GitBranchInfo> branches = new Dictionary<string, GitBranchInfo>();
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
		public BuildStage_CalcVersion (CISession ciSession) : base(BuildStageStatic.STAGE_CALCVERSION, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_RESTORE);
		}


		/// <summary>
		/// Runs the Calculate Version process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess () {
			currentBranchName = CISession.GitProcessor.CurrentBranch.ToLower();

			GitBranchInfo comparisonBranch = null;


			string branchToFind = "";
			if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) branchToFind = "alpha";
			if ( CISession.PublishTarget == PublishTargetEnum.Beta ) branchToFind = "beta";

			if ( CISession.PublishTarget == PublishTargetEnum.Production ) branchToFind = CISession.GitProcessor.MainBranchName;

			// Make sure branch exists.
			if ( !CISession.GitBranches.TryGetValue(branchToFind, out comparisonBranch) )
				ControlFlow.Assert(true == false, "The destination branch [" + branchToFind + "] does not exist.  You must manually create this branch.");

			// Get Main Branch info
			mainBranch = CISession.GitBranches [CISession.GitProcessor.MainBranchName];
			GitBranchInfo currentBranch = CISession.GitBranches [CISession.GitProcessor.CurrentBranch];


			// If version has been set by user manually, then we validate it and set it.
			if ( CISession.ManuallySetVersion != null ) {
				ControlFlow.Assert(CISession.ManuallySetVersion > currentBranch.LatestSemVersionOnBranch,
				                   "Manually set version must be greater than the most current version on the branch deploying to");
				string releaseType = "";
				
				if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) releaseType = "alpha";
				else if (CISession.PublishTarget == PublishTargetEnum.Beta) releaseType = "beta";
				else if ( CISession.PublishTarget == PublishTargetEnum.Production ) {
					newVersion = CISession.ManuallySetVersion;
					CISession.VersionInfo = new VersionInfo(newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
					return StageCompletionStatusEnum.Success;
				}
				SemVersionPreRelease semPre = new SemVersionPreRelease(releaseType, 0, IncrementTypeEnum.None);
				newVersion = new SemVersion(CISession.ManuallySetVersion.Major, CISession.ManuallySetVersion.Minor, CISession.ManuallySetVersion.Patch, semPre.Tag());
				CISession.VersionInfo = new VersionInfo(newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
				AOT_Info("Success:  Manual Version Set:  " + newVersion);
				return StageCompletionStatusEnum.Success;
			}


			// If the top commit on branch is already version tagged, then we are assuming they want to
			// finish off later steps that might not have run successfully previously.
			if ( (CISession.PublishTarget == PublishTargetEnum.Production && currentBranch.Name == mainBranch.Name) || CISession.PublishTarget != PublishTargetEnum.Production ) {
				SemVersion mostCurrentSemVerOnBranch = currentBranch.LatestCommitOnBranch.GetGreatestVersionTag();
				SemVersion zero = new SemVersion(0, 0, 0);
				if ( mostCurrentSemVerOnBranch > zero ) {
					CISession.WasPreviouslyCommitted = true;
					newVersion = mostCurrentSemVerOnBranch;
					CISession.VersionInfo = new VersionInfo(newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
					AOT_Warning("No changes require a version change.  Assuming this is a continuation of a prior post compile failure.");
					AOT_Info("No Version Change!  Existing Version is:  " + newVersion);
					return StageCompletionStatusEnum.Success;
				}
			}


			// Determine the potential version bump...
			incrementPatch = (currentBranchName.StartsWith("fix") || currentBranchName.StartsWith("bug"));
			incrementMinor = (currentBranchName.StartsWith("feature") || currentBranchName.StartsWith("feat"));
			incrementMajor = (currentBranchName.StartsWith("major"));

			// Set IncrementPatch if it is not a key branch name and none of the above is set.
			if ( !incrementMajor && !incrementMinor && !incrementPatch ) {
				if ( currentBranchName != "alpha" && currentBranchName != "beta" && currentBranchName != CISession.GitProcessor.MainBranchName )
					incrementPatch = true;
			}

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
			CISession.VersionInfo = new VersionInfo(newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
			
			AOT_Info("New Version is:  " + newVersion);

			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		/// Calculate version of project when Target is Beta
		/// </summary>
		/// <param name="comparisonBranch"></param>
		private void CalculateBetaVersion (GitBranchInfo comparisonBranch) {
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
		/// <param name="comparisonBranch"></param>
		private void CalculateAlphaVersion (GitBranchInfo comparisonBranch) {
			// See if main is newer and if it's tag is newer.  If so we need to set the comparison's
			// tag to the main version and then add the alpha / beta to it...
			SemVersion tempVersion = CompareToMainVersion(mostRecentBranchTypeVersion, comparisonBranch.Name);


			// If this is the first Version tag on an alpha branch then create new.
			SemVersionPreRelease semPre;
			if ( tempVersion.Prerelease != string.Empty ) 
				semPre = new SemVersionPreRelease(tempVersion.Prerelease);
			else 
				semPre = new SemVersionPreRelease("alpha",0,IncrementTypeEnum.None);
			

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

				tempVersion = new SemVersion(major, minor, patch, comparisonBranchName + "-0000");
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
