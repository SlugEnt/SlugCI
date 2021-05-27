using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;

namespace Slug.CI.SlugBuildStages
{
	class BuildStage_GitCleanup : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_GitCleanup (CISession ciSession) : base(BuildStageStatic.STAGE_GITCLEAN, ciSession) { }


		/// <summary>
		/// Run GIT Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess() {
			List<RecordBranchLatestCommit> branches = CISession.GitProcessor.GetAllBranchesWithLatestCommit();

			// We process main, alpha and beta.
			foreach ( RecordBranchLatestCommit recordBranchLatestCommit in branches ) {
				if ( (recordBranchLatestCommit.branch == CISession.GitProcessor.MainBranchName) ||
				     recordBranchLatestCommit.branch == "alpha" ||
				     recordBranchLatestCommit.branch == "beta" ) {

					GitBranchInfo branchInfo = new GitBranchInfo(recordBranchLatestCommit, CISession.GitProcessor);
					List<string> mergedBranches = CISession.GitProcessor.BranchMerged(recordBranchLatestCommit.branch,branchInfo.LatestCommitOnBranch.CommitHash);
					foreach ( string mergedBranch in mergedBranches ) {
						if ( mergedBranch != "main" && mergedBranch != "master" && mergedBranch != "alpha" && mergedBranch != "beta" ) {
							try {
								if ( !CISession.GitProcessor.DeleteBranch(mergedBranch, true) ) CISession.GitProcessor.DeleteBranch(mergedBranch, false);

							}
							catch ( Exception e ) {
								if ( !e.Message.Contains("remote ref does not exist") ) throw e;
							}
						}
					}
				}
			}

			CISession.GitProcessor.FetchPrune();

			return StageCompletionStatusEnum.Success;
		}
	}
}
