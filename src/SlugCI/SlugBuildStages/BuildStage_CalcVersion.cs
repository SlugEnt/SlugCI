using Nuke.Common;
using Semver;
using Slug.CI.NukeClasses;
using System;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("Test_SlugCI")]


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Cleaning stage
	/// </summary>
	public class BuildStage_CalcVersion : BuildStage {
		
		//private Dictionary<string, GitBranchInfo> branches = new Dictionary<string, GitBranchInfo>();
		private string _currentBranchName;
		private GitBranchInfo _mainBranch;

		private bool _incrementMinor;
		private bool _incrementPatch;
		private bool _incrementMajor;


		private SemVersion _newVersion = null;
		private SemVersion _mostRecentBranchTypeVersion;


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
			_currentBranchName = CISession.GitProcessor.CurrentBranch.ToLower();

			GitBranchInfo comparisonBranch = null;


			string branchToFind = "";
			if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) branchToFind = "alpha";
			if ( CISession.PublishTarget == PublishTargetEnum.Beta ) branchToFind = "beta";

			if ( CISession.PublishTarget == PublishTargetEnum.Production ) branchToFind = CISession.GitProcessor.MainBranchName;

			// Make sure branch exists.
			if ( !CISession.GitBranches.TryGetValue(branchToFind, out comparisonBranch) )
				ControlFlow.Assert(true == false, "The destination branch [" + branchToFind + "] does not exist.  You must manually create this branch.");

			// Get Main Branch info
			_mainBranch = CISession.GitBranches [CISession.GitProcessor.MainBranchName];
			GitBranchInfo currentBranch = CISession.GitBranches [CISession.GitProcessor.CurrentBranch];


			// If version has been set by user manually, then we validate it and set it.
			if ( CISession.ManuallySetVersion != null ) {
				ControlFlow.Assert(CISession.ManuallySetVersion > currentBranch.LatestSemVersionOnBranch,
				                   "Manually set version must be greater than the most current version on the branch deploying to");
				string releaseType = "";
				
				if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) releaseType = "alpha";
				else if (CISession.PublishTarget == PublishTargetEnum.Beta) releaseType = "beta";
				else if ( CISession.PublishTarget == PublishTargetEnum.Production ) {
					_newVersion = CISession.ManuallySetVersion;
					CISession.VersionInfo = new VersionInfo(_newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
					return StageCompletionStatusEnum.Success;
				}
				SemVersionPreRelease semPre = new SemVersionPreRelease(releaseType, 0, IncrementTypeEnum.None);
				_newVersion = new SemVersion(CISession.ManuallySetVersion.Major, CISession.ManuallySetVersion.Minor, CISession.ManuallySetVersion.Patch, semPre.Tag());
				CISession.VersionInfo = new VersionInfo(_newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
				AOT_Info("Success:  Manual Version Set:  " + _newVersion);
				return StageCompletionStatusEnum.Success;
			}


			// If the top commit on branch is ALREADY VERSION tagged, then we are assuming they want to
			// finish off later steps that might not have run successfully previously due to some error.
			if ( (CISession.PublishTarget == PublishTargetEnum.Production && currentBranch.Name == _mainBranch.Name) || CISession.PublishTarget != PublishTargetEnum.Production ) {
				SemVersion mostCurrentSemVerOnBranch = currentBranch.LatestCommitOnBranch.GetGreatestVersionTag();
				SemVersion zero = new SemVersion(0, 0, 0);
				if ( mostCurrentSemVerOnBranch > zero ) {
					CISession.WasPreviouslyCommitted = true;
					_newVersion = mostCurrentSemVerOnBranch;
					CISession.VersionInfo = new VersionInfo(_newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
					AOT_Warning("No changes require a version change.  Assuming this is a continuation of a prior post compile failure.");
					AOT_Info("No Version Change!  Existing Version is:  " + _newVersion);
					return StageCompletionStatusEnum.Success;
				}
			}

			// Get most recent Version Tag for the desired branch type
			_mostRecentBranchTypeVersion = GetMostRecentVersionTagOfBranch(currentBranch);



			//=============================================================
			SemVersion nextVersion = CalculateNextVersion(_mainBranch.LatestSemVersionOnBranch, currentBranch.LatestSemVersionOnBranch, CISession.PublishTarget,
			                                              currentBranch.Name, _mainBranch.Name, CISession.SlugCIConfigObj.UseYearMonthSemVersioning);
			//=============================================================
			return StageCompletionStatusEnum.Failure;


			// Determine the potential version bump...
			if ( !CISession.SlugCIConfigObj.UseYearMonthSemVersioning ) {
				_incrementPatch = (_currentBranchName.StartsWith("fix") || _currentBranchName.StartsWith("bug"));
				_incrementMinor = (_currentBranchName.StartsWith("feature") || _currentBranchName.StartsWith("feat"));
				_incrementMajor = (_currentBranchName.StartsWith("major"));
			}

			// Set IncrementPatch if it is not a key branch name and none of the above is set.
			if ( !_incrementMajor && !_incrementMinor && !_incrementPatch ) {
				if ( _currentBranchName != "alpha" && _currentBranchName != "beta" && _currentBranchName != CISession.GitProcessor.MainBranchName )
					_incrementPatch = true;
			}

			string versionPreReleaseName = "alpha";
			if ( CISession.PublishTarget == PublishTargetEnum.Beta ) versionPreReleaseName = "beta";
			
			

			if ( CISession.PublishTarget== PublishTargetEnum.Production) {
				CalculateMainVersion(_mostRecentBranchTypeVersion);
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
			CISession.VersionInfo = new VersionInfo(_newVersion, currentBranch.LatestCommitOnBranch.CommitHash);
			
			AOT_Info("New Version is:  " + _newVersion);

			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		/// If UseYearScheme:
		/// 
		/// </summary>
		/// <param name="currentMain"></param>
		/// <param name="currentBranch"></param>
		/// <param name="publishTarget"></param>
		/// <param name="currentBranchName"></param>
		/// <param name="mainBranchName"></param>
		/// <param name="useYearMonthScheme"></param>
		/// <returns></returns>
		internal SemVersion CalculateNextVersion (SemVersion currentMain,
		                               SemVersion currentBranch,
		                               PublishTargetEnum publishTarget,
		                               string currentBranchName,
		                               string mainBranchName,
		                               bool useYearMonthScheme) {
			SemVersion nextVersion = new(0);


			// If we are using Year/Month Semversioning 
			if ( useYearMonthScheme )
			{
				_incrementPatch = false;
				DateTime currentDate = DateTime.Now;
				int patchNum = 0;

				// If current date is same as last versions year and month, then patchnum is same, otherwise its zero
				if (_mostRecentBranchTypeVersion.Major == currentDate.Year && _mostRecentBranchTypeVersion.Minor == currentDate.Month)
					patchNum = _mostRecentBranchTypeVersion.Patch;

				SemVersion newestVersion = new SemVersion(currentDate.Year, currentDate.Month, patchNum);
			}

			return nextVersion;
		}

		/// <summary>
		/// There is an issue whereby if an alpha / beta branch has been merged into main branch, it will list the #.#.#-alpha as the most current.
		/// It should be #.#.#+1 as the most current.  This fixes this.
		/// </summary>
		/// <param name="branch"></param>
		/// <returns></returns>
		public SemVersion GetMostRecentVersionTagOfBranch (GitBranchInfo branch) {
			SemVersion tempVersion = CISession.GitProcessor.GetMostRecentVersionTagOfBranch(branch.Name);
			if ( branch.LatestSemVersionOnBranch >= tempVersion ) {
				_incrementPatch = true;
				return branch.LatestSemVersionOnBranch;
			}
			return tempVersion;
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
			if ( _mostRecentBranchTypeVersion > newestVersion ) {
				newestVersion = _mostRecentBranchTypeVersion;
				updatedBeta = new SemVersionPreRelease(newestVersion.Prerelease);
			}
			else {
				// Get pre-release of the newest version, which is not a beta branch
				SemVersionPreRelease preOld = new SemVersionPreRelease(newestVersion.Prerelease);

				// Get pre-release of most current beta branch version
				SemVersionPreRelease currentBeta = new SemVersionPreRelease(_mostRecentBranchTypeVersion.Prerelease);

				// Update the beta to be of the Increment Type
				if ( currentBeta.IncrementType < preOld.IncrementType )
					updatedBeta = new SemVersionPreRelease(currentBeta.ReleaseType, currentBeta.ReleaseNumber, preOld.IncrementType);
				else {
					updatedBeta = currentBeta;
				}
				newestVersion = new SemVersion(newestVersion.Major, newestVersion.Minor, newestVersion.Patch, updatedBeta.Tag());
			}


			// Increase version
			if (_incrementMinor) updatedBeta.BumpMinor();
			else if (_incrementPatch) updatedBeta.BumpPatch();
			else if (_incrementMajor) updatedBeta.BumpMajor();
			else updatedBeta.BumpVersion();

			_newVersion = new SemVersion(newestVersion.Major, newestVersion.Minor, newestVersion.Patch, updatedBeta.Tag());
		}


		/// <summary>
		/// Computes the version for the Alpha Branch.
		/// </summary>
		/// <param name="comparisonBranch"></param>
		private void CalculateAlphaVersion (GitBranchInfo comparisonBranch) {
			// See if main is newer and if it's tag is newer.  If so we need to set the comparison's
			// tag to the main version and then add the alpha / beta to it...
			SemVersion tempVersion = CompareToMainVersion(_mostRecentBranchTypeVersion, comparisonBranch.Name);


			// If this is the first Version tag on an alpha branch then create new.
			SemVersionPreRelease semPre;
			if ( tempVersion.Prerelease != string.Empty ) 
				semPre = new SemVersionPreRelease(tempVersion.Prerelease);
			else 
				semPre = new SemVersionPreRelease("alpha",0,IncrementTypeEnum.None);
			

			if (_incrementMinor) semPre.BumpMinor();
			else if (_incrementPatch) semPre.BumpPatch();
			else if (_incrementMajor) semPre.BumpMajor();
			else semPre.BumpVersion();

			_newVersion = new SemVersion(tempVersion.Major, tempVersion.Minor, tempVersion.Patch, semPre.Tag());
		}


		SemVersion CompareToMainVersion (SemVersion comparisonVersion,string comparisonBranchName) {
			SemVersion tempVersion;

			if (_mainBranch.LatestSemVersionOnBranch >= comparisonVersion)
			{
				int major = _mainBranch.LatestSemVersionOnBranch.Major;
				int minor = _mainBranch.LatestSemVersionOnBranch.Minor;
				int patch = _mainBranch.LatestSemVersionOnBranch.Patch;
				if (_incrementMajor)
				{
					major++;
					minor = 0;
					patch = 0;
				}
				if (_incrementMinor)
				{
					minor++;
					patch = 0;
				}
				if (_incrementPatch) patch++;

				tempVersion = new SemVersion(major, minor, patch, comparisonBranchName + "-0000");
			}
			else
				tempVersion = new SemVersion(comparisonVersion.Major, comparisonVersion.Minor, comparisonVersion.Patch, comparisonVersion.Prerelease);

			return tempVersion;
		}


		/// <summary>
		/// Calculations to compute the new version when it's a production push.
		/// </summary>
		private void CalculateMainVersion (SemVersion comparisonVersion) {
			// Strip out the prerelease portion to determine if main is newer or not
			SemVersion tempVersion =
				new SemVersion(comparisonVersion.Major, comparisonVersion.Minor, comparisonVersion.Patch);
			if (_mainBranch.LatestSemVersionOnBranch < tempVersion) _newVersion = new SemVersion(tempVersion.Major, tempVersion.Minor, tempVersion.Patch);
			else
			{
				int major = _mainBranch.LatestSemVersionOnBranch.Major;
				int minor = _mainBranch.LatestSemVersionOnBranch.Minor;
				int patch = _mainBranch.LatestSemVersionOnBranch.Patch;
				if (_incrementMinor)
				{
					minor++;
					patch = 0;
				}

				if (_incrementPatch) patch++;
				_newVersion = new SemVersion(major, minor, patch);
			}
		}
	

	}
}
