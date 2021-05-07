using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;


namespace Slug.CI
{
	/// <summary>
	///  Processes all Git Related stuff 
	/// </summary>
	class GitProcessor {
		const string COMMIT_MARKER = "|^|";
		const string GIT_COMMAND_MARKER = "|";

		internal string DotNetPath { get; set; }
		

		/// <summary>
		/// Session settings and data
		/// </summary>
		private CISession CISesion { get; set; }
		
		/// <summary>
		/// The Branch that the repository is currently on
		/// </summary>
		public string CurrentBranch { get; private set; }
		public string Version { get; private set; }
		public string SemVersion { get; private set; }
		public string SemVersionNugetCompatible { get; private set; }
		public string InformationalVersion { get; private set; }
		public string GitTagName { get; private set; }
		public string GitTagDesc { get; private set; }

		List<string> _versionList = new List<string>();


		/// <summary>
		/// Returns True if the current branch is the Main Branch.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrentBranchMainBranch () { return IsMainBranch(CurrentBranch);}


		/// <summary>
		/// Returns True if the given branch name is considered the Main branch.
		/// </summary>
		/// <param name="branchName"></param>
		/// <returns></returns>
		public static bool IsMainBranch(string branchName) {
			string lcBranch = branchName.ToLower();
			if ( lcBranch == "master" || lcBranch == "main" )
				return true;
			else
				return false;
		}


		/// <summary>
		/// Will tell you if the Version had been previously committed to Git.  This means that we are possibly only doing these steps to get to a later
		/// step (such as pack or publish) that might have previously failed for some reason (Bad password, userId, path, etc)
		/// </summary>
		public bool WasVersionPreviouslyCommitted { get; private set; }


		/// <summary>
		/// Keeps track of all of the Git Command output for debugging purposes.
		/// </summary>
		public List<Output> GitCommandOutputHistory = new List<Output>();


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="gitVersion"></param>
		public GitProcessor (CISession ciSession) {
			CISesion = ciSession;

			DotNetPath = ToolPathResolver.GetPathExecutable("dotnet");

			IdentifyMainBranch();
			Fetch_GitVersion();
			PrintGitCommandVersion();
		}


		/// <summary>
		/// Prints the current version of the Git Command being used.  Version is only shown when PrintGitHistory is called.
		/// </summary>
		private void PrintGitCommandVersion () {
			List<Output> gitOutput;

			try {
				string gitArgs = "--version";
				ControlFlow.Assert(ExecuteGit(gitArgs, out gitOutput) == true, "PrintGitCommandVersion:::  .Git Command Failed:  git " + gitArgs);
			}
			catch (Exception e) { 
				PrintGitHistory();
				throw e;
			}
		}



		/// <summary>
		/// Gets the current branch that the project is on
		/// </summary>
		/// <returns></returns>
		public string GetCurrentBranch () {
			try { 
				string cmdArgs = "branch --show-current";
				if (!ExecuteGit(cmdArgs, out List<Output> output)) throw new ApplicationException("GetCurrentBranch::: Git Command failed:  git " + cmdArgs);
				CurrentBranch = output.First().Text;
				return CurrentBranch;
			}
			catch (Exception e)
			{
				PrintGitHistory();
				if ( e.Message.Contains("Sequence contains no elements") ) 
					throw new InvalidOperationException("Do you have a branch checked out?  Original error: " + e.Message);
				throw e;
			}

		}



		/// <summary>
		/// Determines if there are any uncommitted changes on the current branch.
		/// </summary>
		/// <returns></returns>
		public bool IsUncommittedChanges () {
			try { 
				string gitArgs = "update-index -q --refresh";
				if (!ExecuteGit_NoOutput(gitArgs)) throw new ApplicationException("IsUncommittedChanges::: Git Command failed:  git " + gitArgs);

				gitArgs = "diff-index --quiet HEAD --";
				if (!ExecuteGit_NoOutput(gitArgs)) throw new ApplicationException("There are uncommited changes on the current branch: " + CurrentBranch +  "  Commit or discard existing changes and then try again.");
				return true;
			}
			catch (Exception e)
			{
				PrintGitHistory();
				throw e;
			}

		}



		/// <summary>
		/// Determines if the the local "Current" branch is up to date with the remote branch.  If not it will throw an error.
		/// </summary>
		/// <param name="branchName"></param>
		/// <returns></returns>
		public bool IsBranchUpToDate (string branchName = null) {
			if ( branchName == null ) branchName = CurrentBranch;

			try {
				string gitArgs = "remote update";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("IsBranchUpToDate::: Git Command failed:  git " + gitArgs);

				gitArgs = "status uno";
				if ( !ExecuteGit_NoOutput(gitArgs) )
					throw new ApplicationException("IsBranchUpToDate::: Your local branch: [" +
					                               branchName +
					                               "] is not up to date with the remote one.  You will need to manually correct and then try again.");
				return true;
			}
			catch (Exception e)
			{
				PrintGitHistory();
				throw e;
			}

		}


		/// <summary>
		/// Used to push the app into a Development commit, which means it is tagged with a SemVer tag, such as 2.5.6-alpha1001
		/// </summary>
		public void CommitSemVersionChanges () {
			try {
				if ( WasVersionPreviouslyCommitted ) return;
				string commitErrStart = "CommitSemVersionChanges:::  Git Command Failed:  git ";
				GitTagName = "Ver" + SemVersion;
				GitTagDesc = "Deployed Version:  " + PrettyPrintBranchName(CurrentBranch) + "  |  " + SemVersion;
				string gitArgs = "";

				gitArgs = string.Format("tag -a {0} -m \"{1}\"", GitTagName, GitTagDesc);
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);

				gitArgs = "push --set-upstream origin " + CurrentBranch;
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);

				gitArgs = "push --tags origin";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw e;
			}
		}


		/// <summary>
		/// Gets the next version and the GitTags
		/// </summary>
		/// <param name="isMainBranchBuild"></param>
		public void GetNextVersionAndTags (bool isMainBranchBuild) {
			string gitArgs;
			string commitErrStart = "GetNextVersionAndTags:::  Git Command Failed:  git ";

			List<Output> gitOutput;

			try {
				GetNextVersion(isMainBranchBuild);
				// Override GitTagName if main branch
				if ( isMainBranchBuild ) {
					GitTagName = "Ver" + Version;

					// Lets validate that local master and remote master are up to date.  If not then no need to proceed with anything else - we will have problems later
					IsBranchUpToDate(MainBranchName);
				}
				else
					GitTagName = "Ver" + SemVersion;


				//GitTagName = "Ver" + Version;
				GitTagDesc = "Deployed Version:  " + PrettyPrintBranchName(CurrentBranch) + "  |  " + Version;

				// See if the Tag exists already, if so we will get errors later, better to stop now.  
				gitArgs = "describe --tags --abbrev=0";

				if ( !ExecuteGit(gitArgs, out gitOutput) ) {
					if ( gitOutput.Count > 0 ) {
						if ( gitOutput [0].Text.Contains("fatal: No names found") )
							return;
						throw new ApplicationException(commitErrStart + gitArgs);
					}
				}

				if (gitOutput.Count > 0 && gitOutput[0].Text == GitTagName)
				{
					WasVersionPreviouslyCommitted = true;
					Logger.Warn("The Git Tag: {0} was previously committed.  We are assuming this is one of 2 things:  1) Just a rebuild of the current branch with no changes.  2) A run to correct a prior error in a later stage.  Certain code sections will be skipped.", GitTagName);
				}
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw e;
			}
		}




		/// <summary>
		/// This is the Main Branch Commit stage
		/// </summary>
		public void CommitMainVersionChanges()
		{
			string gitArgs;
			List<Output> gitOutput;

			// This is not an update, it is a redo of a previous run that may have errored or its a clean run, but no changes have been committed.  So we skip this.
			if ( WasVersionPreviouslyCommitted ) return;

			try {
				// Checkout main and merge the current branch in
				if (!IsCurrentBranchMainBranch())
				{
					gitArgs = "checkout " + MainBranchName;
					if (!ExecuteGit_NoOutput(gitArgs)) throw new ApplicationException("CommitMainVersionChanges:::  .Git Command failed:  git " + gitArgs);

					gitArgs = string.Format("merge {0} --no-ff --no-edit -m \"Merging Branch: {0}   |  {1}\"", CurrentBranch, GitVersion.MajorMinorPatch);
					if (!ExecuteGit_NoOutput(gitArgs)) throw new ApplicationException("CommitMainVersionChanges:::  .Git Command failed:  git " + gitArgs);
				}


				gitArgs = "add .";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitVersionChanges:::  .Git Command failed:  git " + gitArgs);

				gitArgs = string.Format("commit -m \"{0} {1}", COMMIT_MARKER, GitTagDesc);
				if ( !ExecuteGit(gitArgs, out gitOutput) ) {
					if (!gitOutput.Last().Text.Contains("nothing to commit"))
						throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);
				}

				gitArgs = string.Format("tag -a {0} -m \"{1}\"", GitTagName, GitTagDesc);
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);

				gitArgs = "push origin ";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);

				gitArgs = "push --tags origin";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);

				// Delete the Feature Branch
				if ( CurrentBranch != MainBranchName ) {
					// See if branch exists on origin.  If not we expect an error below.
					gitArgs = "ls-remote --exit-code --heads origin " + CurrentBranch;
					bool bErrorIsExpected = false;
					if ( !ExecuteGit_NoOutput(gitArgs) ) bErrorIsExpected = true;

					gitArgs = "branch -d " + CurrentBranch;
					if ( !ExecuteGit_NoOutput(gitArgs) && !bErrorIsExpected ) {
						ControlFlow.AssertWarn(1 ==0,"Unable to delete the local branch [" + CurrentBranch +  "see Git Errors below.  Will continue to process since this is not a fatal error.  You may need to perform branch cleanup manually."); 
						PrintGitHistory();
						// We will probably fail the next error too...
						bErrorIsExpected = true;
					}

					gitArgs = "push origin --delete " + CurrentBranch;
					if ( !ExecuteGit_NoOutput(gitArgs) ) 
						if (!bErrorIsExpected)
							throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);
					Logger.Success("The previous 2 commits will issue errors if the local branch was never pushed to origin.  They can be safely ignored.");
				}
			}
			catch (Exception e)
			{
				PrintGitHistory();
				throw e;
			}
		}

		/// <summary>
		/// Executes the Git Command, returning ONLY true or false to indicate success or failure
		/// </summary>
		/// <param name="cmdArguments"></param>
		/// <returns></returns>
		private bool ExecuteGit_NoOutput (string cmdArguments) {
			string command = "git";

			// Log it
			Output output =new Output();
			output.Text = GIT_COMMAND_MARKER +  command + " " + cmdArguments;
			GitCommandOutputHistory.Add(output);

			IProcess process = ProcessTasks.StartProcess(command, cmdArguments, CISesion.RootDirectory, logOutput: false);
			process.AssertWaitForExit();

			// Copy output to history.
			GitCommandOutputHistory.AddRange(process.Output);

			if (process.ExitCode != 0) return false;
			return true;
		}


		/// <summary>
		/// Executes the requested Git Command AND returns the output.  Returns True on success, false otherwise
		/// </summary>
		/// <param name="cmdArguments"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		private bool ExecuteGit (string cmdArguments, out List<Output> output) {
			string command = "git";


			// Log it
			Output outputCmd = new Output();
			outputCmd.Text = GIT_COMMAND_MARKER + command + " " + cmdArguments;
			GitCommandOutputHistory.Add(outputCmd);

			IProcess process = ProcessTasks.StartProcess(command, cmdArguments, CISesion.RootDirectory, logOutput: false);
			process.AssertWaitForExit();
			output = process.Output.ToList();

			// Copy output to history.
			GitCommandOutputHistory.AddRange(process.Output);

			if ( process.ExitCode != 0 ) return false;
			return true;
		}


		public void GetNextVersion (bool isMainBranchBuild) {
			Version = GitVersion.MajorMinorPatch;
			SemVersion = GitVersion.SemVer;

			/* GitVersion Is not working correctly.
			InformationalVersion = GitVersion.InformationalVersion;
			SemVersionNugetCompatible = GitVersion.NuGetVersionV2;

			return;
			*/

			if ( isMainBranchBuild ) {
				SemVersion = Version;
				SemVersionNugetCompatible = Version;
				InformationalVersion = Version + "+" + GitVersion.Sha.Substring(0, 7);
				return;
			}


			string label = GitVersion.PreReleaseLabel;
			int commitNumber = GetBranchCommitCount();
			ControlFlow.Assert(commitNumber > 0, "There are no commits on branch [" + CurrentBranch + "].  Since this is a non-main branch there is nothing to do.");

			//int commitNumber = Int32.Parse(GitVersion.CommitsSinceVersionSource);
			//commitNumber++;
			SemVersion = Version + "-" + label + "." + commitNumber;
			
			// Calculate Nuget Version
			string zeros = "";
			if ( commitNumber > 99 ) zeros = "0";
			else if ( commitNumber > 9 )
				zeros = "00";
			else  zeros = "000";
			SemVersionNugetCompatible = Version + "-" + label +  zeros + commitNumber;

			InformationalVersion = SemVersion + "+" + GitVersion.Sha.Substring(0, 7);
		}


		/// <summary>
		/// Returns the number of commits on the current branch.
		/// </summary>
		/// <returns></returns>
		public int GetBranchCommitCount () {
			string gitArgs = string.Format("reflog show --no-abbrev {0}", CurrentBranch);
			if (ExecuteGit(gitArgs, out List<Output> gitOutput)) {
				return gitOutput.Count - 1;
			}
			ControlFlow.Assert(1 == 0, "Unable to determine how many commits are on current branch.");
			return 0;
		}


		/// <summary>
		/// Prints the history of the Git commands to the console.
		/// </summary>
		private void PrintGitHistory () {
			
			Console.WriteLine("");
			Console.WriteLine("Git Command Execution History is below for debugging purposes",Color.DeepSkyBlue);
			foreach ( Output line in GitCommandOutputHistory ) {
				if ( line.Text.StartsWith(GIT_COMMAND_MARKER) ) 
					Console.WriteLine("  " + line.Text.Substring(1), Color.Orange);
				else 
					Console.WriteLine("     "  + line.Text,Color.DarkKhaki);
				
			}

		}



		/// <summary>
		/// Sets up and queries the GitVersion.
		/// </summary>
		public void Fetch_GitVersion () {
			Misc.WriteSubHeader("GitVersion:  Fetch");

			GitVersionSettings settings = new GitVersionSettings()
			{
				ProcessWorkingDirectory = CISesion.RootDirectory,
				Framework = "netcoreapp3.1",
				NoFetch = false,
				ProcessLogOutput = true,
				Verbosity = CISesion.VerbosityGitVersion,
				UpdateAssemblyInfo = true,
			};

			(GitVersion result,IReadOnlyCollection<Output> output) = GitVersionTasks.GitVersion(settings);

		}


		/// <summary>
		/// Returns the GitVersion object
		/// </summary>
		public GitVersion GitVersion { get; private set; }


		/// <summary>
		/// Determines whether Main or Master is the "main" branch.
		/// </summary>
		private void IdentifyMainBranch () {
			try { 
				string gitArgs = "branch";
				if (!ExecuteGit(gitArgs, out List<Output> output)) throw new ApplicationException("IdentifyMainBranch:::   .Git Command failed:  git " + gitArgs);

				char [] skipChars = new [] {' ', '*'};

				bool found = false;
				foreach ( Output branch in output ) {
					string branchName = branch.Text.TrimStart(skipChars).TrimEnd();
					if ( IsMainBranch(branchName))  {
						if ( found )
							throw new ApplicationException(
								"Appears to be a main and master branch in the repository.  This is not allowed.  Please cleanup the repo so only master or only main exists.");
						found = true;
						MainBranchName = branchName;
					}
				}
			}
			catch (Exception e)
			{
				PrintGitHistory();
				throw e;
			}

		}



		/// <summary>
		/// adds spaces between every slash it finds in the branch name.
		/// </summary>
		/// <param name="branch"></param>
		/// <returns></returns>
		public string PrettyPrintBranchName (string branch) {
			string[] parts = branch.Split('/');
			string newName = "";
			foreach ( string item in parts ) {
				if ( newName != string.Empty )
					newName = newName + " / " + item;
				else
					newName = item;
			}

			return newName;
		}


		/// <summary>
		/// The name of the main branch in the repository.
		/// </summary>
		public string MainBranchName { get; private set; }
	}
}
