using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Slug.CI.NukeClasses;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// This stage commits the version bump and other changes to production...
	/// </summary>
	class BuildStage_GitCommit : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_GitCommit(CISession ciSession) : base(BuildStageStatic.STAGE_GITCOMMIT, ciSession) { }


		/// <summary>
		/// Run The Git Commit process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			// Set Commit tag and description
			string versionTag = "Ver" + CISession.SemVersion;
			string gitTagDesc = "Deployed Version:  " + PrettyPrintBranchName(CISession.GitProcessor.CurrentBranch) + "  |  " + CISession.SemVersion;

			if (CISession.PublishTarget == PublishTargetEnum.Production)
				CISession.GitProcessor.CommitMainVersionChanges(versionTag,gitTagDesc);
			else
				CISession.GitProcessor.CommitSemVersionChanges(versionTag,gitTagDesc);

			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		/// adds spaces between every slash it finds in the branch name.
		/// </summary>
		/// <param name="branch"></param>
		/// <returns></returns>
		public string PrettyPrintBranchName(string branch)
		{
			string[] parts = branch.Split('/');
			string newName = "";
			foreach (string item in parts)
			{
				if (newName != string.Empty)
					newName = newName + " / " + item;
				else
					newName = item;
			}

			return newName;
		}
	}
}
