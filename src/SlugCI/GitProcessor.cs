﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;
using Semver;
using Slug.CI.NukeClasses;
using SlugEnt.CmdProcessor;
using Console = Colorful.Console;

[assembly: InternalsVisibleTo("Test_SlugCI")]

namespace Slug.CI
{

	/// <summary>
	/// Identified Remotes
	/// </summary>
	public record Remotes (string name, string url, string operation);


	/// <summary>
	/// Branch / Commit Information
	/// </summary>
	public record RecordBranchLatestCommit (bool isCheckedOutBranch, string branch, string commitHash, string commitMsg, bool isLocal);


	/// <summary>
	/// Information returned from the Git Describe --tags command.
	/// </summary>
	public record RecordGitDescribeTag (string tag, int commitsSince, string commitHash);


	/// <summary>
	///  Processes all Git Related stuff 
	/// </summary>
	public class GitProcessor {
		private const string GIT_COMMAND = "git ";
		public const string GIT_SHOW_SLUGCI = "M:slugci";
		public const string GIT_SHOW_COMMIT = "commit:";
		public const string GIT_SHOW_PARENTS = "parents:";
		public const string GIT_SHOW_COMMITTER = "committer";
		public const string GIT_SHOW_COMMITTED_DATE = "cdate:";
		public const string GIT_SHOW_MESSAGE = "msg:";
		public const string GIT_SHOW_REFS = "refs:";

		const string COMMIT_MARKER = "|^|";
		const string GIT_COMMAND_MARKER = "|";


		/// <summary>
		/// Session settings and data
		/// </summary>
		private CISession CISession { get; set; }


		/// <summary>
		/// Used only for debugging the length of time to run Git Commands
		/// </summary>
		public List<GitProcessLogTime> ProcessTimeLogs { get; set; } = new();


		/// <summary>
		/// The Branch that the repository is currently on
		/// </summary>
		public string CurrentBranch { get; internal set; }


		public bool IsLocalBranchUptoDate { get; private set; }
		public bool AreUncommitedChangesOnLocalBranch { get; private set; }
		public string GitCommandVersion { get; private set; }

		/// <summary>
		/// If true, the current path and the command being executed are displayed.
		/// </summary>
		public bool logInvocationLogging { get; private set; }

		/// <summary>
		/// If true, the output of Git commands is displayed.
		/// </summary>
		public bool logOutputLogging { get; private set; }

		public List<RecordBranchLatestCommit> AllBranchInfo { get; private set; } = new List<RecordBranchLatestCommit>();

		/// <summary>
		/// Returns what the "main" branch name is.  This is typically main for newer repos and master for older.
		/// </summary>
		public string MainBranchName { get; private set; }


		/// <summary>
		/// Returns True if the current branch is the Main Branch.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrentBranchMainBranch () { return IsMainBranch(CurrentBranch); }


		/// <summary>
		/// Returns True if the given branch name is considered the Main branch.
		/// </summary>
		/// <param name="branchName"></param>
		/// <returns></returns>
		public bool IsMainBranch (string branchName) { return branchName == MainBranchName; }


		/// <summary>
		/// Will tell you if the Version had been previously committed to Git.  This means that we are possibly only doing these steps to get to a later
		/// step (such as pack or publish) that might have previously failed for some reason (Bad password, userId, path, etc)
		/// </summary>
		public bool WasVersionPreviouslyCommitted { get; private set; }


		/// <summary>
		/// Keeps track of all of the Git Command output for debugging purposes.
		/// </summary>
		public List<LineOutColored> GitCommandOutputHistory = new List<LineOutColored>();


		/// <summary>
		/// True if an internal GIT command errored.
		/// </summary>
		public bool HasErrored { get; private set; }


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession"></param>
		public GitProcessor (CISession ciSession) {
			CISession = ciSession;

			if ( CISession.VerbosityGitVersion == ProcessVerbosity.Nothing) {
				logInvocationLogging = false;
				logOutputLogging = false;
			}
			else if (ciSession.VerbosityGitVersion == ProcessVerbosity.Commands ) {
				logInvocationLogging = true;
				logOutputLogging = false;
			}
			else {
				logInvocationLogging = true;
				logOutputLogging = true;
			}
		}


		/// <summary>
		/// Perform Git Processing startup processes
		/// </summary>
		public async Task Startup () {
			Task GetGitTask =  GetGitCommandVersionAsync();

			// Get some basic Git info about the repository.
			Task RepoInfoTask = GetRepositoryInfoAsync();

			GetGitTask.Wait();
			RepoInfoTask.Wait();
		}


		/// <summary>
		/// Gets basic info about the repository
		/// </summary>
		public async Task GetRepositoryInfoAsync () {
			RefreshLocalBranchStatus();
			GetMainBranchName();

			GetCurrentBranch();
			RefreshUncommittedChanges();

			FetchOriginToLocal(MainBranchName);
		}


		/// <summary>
		/// Prints the current version of the Git Command being used.  Version is only shown when PrintGitHistory is called.
		/// </summary>
		private async Task GetGitCommandVersionAsync () {
			Stopwatch stopwatch = new();
			stopwatch.Start();
			
			List<LineOutColored> gitOutput;
			string gitArgs = "--version";
			ExecuteGitTryCatch("GetGitCommandVersion", gitArgs, out gitOutput);
			if ( gitOutput [0].Text.StartsWith("git version") ) { GitCommandVersion = gitOutput [0].Text.Substring(12); }
			else
				ControlFlow.Assert(true == false, "Retrieving Git --Version returned an invalid value of - " + gitOutput [0].Text);

			stopwatch.Stop();
			ProcessTimeLogs.Add(new GitProcessLogTime("GetGitCommandVersion",gitArgs,stopwatch.ElapsedMilliseconds));
			return;
		}



		/// <summary>
		/// Gets the current branch that the project is on
		/// </summary>
		/// <returns></returns>
		public string GetCurrentBranch () {
			try {
				Stopwatch stopwatch = new();
				stopwatch.Start();

				string cmdArgs = "branch --show-current";
				if ( !ExecuteGit(cmdArgs, out List<LineOutColored> output) ) throw new ApplicationException("GetCurrentBranch::: Git Command failed:  git " + cmdArgs);
				CurrentBranch = output.First().Text;

				stopwatch.Stop();
				ProcessTimeLogs.Add(new GitProcessLogTime("GetCurrentBranch", cmdArgs, stopwatch.ElapsedMilliseconds));
				return CurrentBranch;
			}
			catch ( Exception e ) {
				PrintGitHistory();
				if ( e.Message.Contains("Sequence contains no elements") )
					throw new InvalidOperationException("Do you have a branch checked out?  Original error: " + e.Message);
				throw new ApplicationException("Unexpected error",e);
			}
		}


		/// <summary>
		/// Retrieves detailed information about a specific commit.  
		/// </summary>
		/// <param name="commitHash">The hash of the commit to retrieve info about</param>
		/// <returns></returns>
		public GitCommitInfo GetCommitInfo (string commitHash) {
			List<LineOutColored> gitOutput;
			gitOutput = ShowCommitNoDiff(commitHash);
			return new GitCommitInfo(gitOutput);
		}



		/// <summary>
		/// Retrieves information about all branches and their latest commit.
		/// </summary>
		/// <param name="refresh"></param>
		/// <returns></returns>
		public List<RecordBranchLatestCommit> GetAllBranchesWithLatestCommit (bool refresh = false) {
			if ( AllBranchInfo.Count != 0 && refresh == false ) return AllBranchInfo;

			List<LineOutColored> gitOutput;
			string gitArgs = "branch --sort=-committerdate --all -vv";
			ExecuteGitTryCatch("GetAllBranchesWithLatestCommit", gitArgs, out gitOutput);

			List<RecordBranchLatestCommit> results = new List<RecordBranchLatestCommit>();
			if ( gitOutput.Count == 0 ) return results;

			// Convert output into record results
			foreach ( LineOutColored output in gitOutput ) {
				bool isLocal = true;
				RecordBranchLatestCommit record;
				if ( output.Text.Substring(0, 1) == "*" ) {
					string [] elements = output.Text.Substring(2).Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
					if ( elements [0].StartsWith("remotes/") ) isLocal = false;
					record = new RecordBranchLatestCommit(true, elements [0], elements [1], elements [2],isLocal);
				}
				else {
					string [] elements = output.Text.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
					if (elements[0].StartsWith("remotes/")) isLocal = false;
					record = new RecordBranchLatestCommit(false, elements [0], elements [1], elements [2],isLocal);
				}

				results.Add(record);
			}

			AllBranchInfo = results;
			return results;
		}


		/// <summary>
		/// Finds the latest Ver#.#.# tag on the specified branch.
		/// Returns a RecordGitDescribeTag record.  
		/// <para>If no version could be found the tag will be set to string.empty</para>
		/// </summary>
		/// <param name="branchName">Branch name to search for the version tag on</param>
		/// <returns></returns>
		private RecordGitDescribeTag FindLatestGitVersionTagOnBranch (string branchName) {
			List<LineOutColored> gitOutput;
			string gitArgs = "describe --tags " + branchName + " --long --match \"Ver*\"";
			ExecuteGit( gitArgs, out gitOutput);
			if ( gitOutput.Count == 0 ) return new RecordGitDescribeTag("", 0, "");
			if ( gitOutput [0].Text.Contains("fatal: No names found") ) return new RecordGitDescribeTag("", 0, "");
			if (gitOutput[0].Text.Contains("fatal: No tags can describe")) return new RecordGitDescribeTag("",0,"");
			return GetGitDescribeTag(gitOutput [0].Text);
		}


		/// <summary>
		/// Converts a GitDescribeTag command result into its component parts in a RecordGitDescribeTag
		/// <para>The record-->tag value is the actual tag contents</para>
		/// <para>The record-->commitsSince is the number of commits since this tag was created.</para>
		/// <para>The record-->commitHash is the id of the commit that this tag is a part of</para>
		/// </summary>
		/// <param name="gitDescribeOutput">The output from the Git Describe --Tags command</param>
		/// <returns>RecordGitDescribeTag</returns>
		internal static RecordGitDescribeTag GetGitDescribeTag (string gitDescribeOutput) {
			int gitMetaIndexStart = gitDescribeOutput.LastIndexOf("-g");
			ControlFlow.Assert(gitMetaIndexStart != -1, "Git Describe out was not in expected format:  [" + gitDescribeOutput + "]");

			gitMetaIndexStart += 2;
			string hash = gitDescribeOutput.Substring(gitMetaIndexStart);

			// Find the commit count
			string substr = gitDescribeOutput.Substring(0, gitMetaIndexStart - 2);
			int commitCountIndex = substr.LastIndexOf('-');
			ControlFlow.Assert(commitCountIndex != -1, "Git Describe was not in in expected format.  Trying to find CommitCount:  [" + gitDescribeOutput + "]");
			bool foundCommitCount = int.TryParse(substr.Substring(commitCountIndex + 1), out int commitCount);
			if ( !foundCommitCount )
				ControlFlow.Assert(foundCommitCount,
				                   "Git Descrive was not in expected format.  Unable to determine the Git Commit Count Since. [" + gitDescribeOutput + "]");

			string tag = gitDescribeOutput.Substring(0, commitCountIndex);
			RecordGitDescribeTag record = new RecordGitDescribeTag(tag, commitCount, hash);
			return record;
		}



		/// <summary>
		/// Finds the latest Ver#.#.# tag on the specified branch returning it in SemVersion format.
		/// If it cannot find the Version tag it will set SemVer to 0.0.0
		/// </summary>
		/// <param name="branchName"></param>
		/// <returns></returns>
		public SemVersion FindLatestSemVersionOnBranch (string branchName) {
			RecordGitDescribeTag record = FindLatestGitVersionTagOnBranch(branchName);
			if ( record.tag == string.Empty ) return new SemVersion(0, 0, 0);
			return ConvertVersionToSemVersion(record.tag);
		}



		/// <summary>
		/// Creates a SemVersion object from a version string read from a git Tag.
		/// </summary>
		/// <param name="version"></param>
		/// <returns></returns>
		public static SemVersion ConvertVersionToSemVersion (string version) {
			char [] separators = new char [] {'.', '-'};

			ControlFlow.Assert(version.StartsWith("Ver"), "A slugCI version tag must start with Ver");

			string [] versionSplit = version.Substring(3).Split(separators, 4);

			ControlFlow.Assert(versionSplit.Length > 2,
			                   "Version [" +
			                   version +
			                   "]  is not in proper format of Ver#.#.#-a#.  To fix this, you may need to make a commit to the branch and manually set a version tag in the format Ver#.#.#.  Be sure and push the change to remote.");

			string alpha = "";
			if ( versionSplit.Length == 4 ) alpha = versionSplit [3];

			// Make sure the first three elements are numbers
			if ( !int.TryParse(versionSplit [0], out int major) ) {
				ControlFlow.Assert(true == false, "Version number is not in proper format.  Major part is not a number:  [" + version + "]");
			}

			if ( !int.TryParse(versionSplit [1], out int minor) ) {
				ControlFlow.Assert(true == false, "Version number is not in proper format.  Minor part is not a number:  [" + version + "]");
			}

			if ( !int.TryParse(versionSplit [2], out int patch) ) {
				ControlFlow.Assert(true == false, "Version number is not in proper format.  Patch part is not a number:  [" + version + "]");
			}

			// If here we can make a Semversion object
			SemVersion semVersion = new SemVersion(major, minor, patch, alpha);
			return semVersion;
		}



		/// <summary>
		/// Determines what the main branch name is.
		/// </summary>
		/// <returns></returns>
		public void GetMainBranchName () {
			Stopwatch stopwatch = new();
			stopwatch.Start();

			List<LineOutColored> gitOutput;
			string gitArgs = "remote show origin";
			ExecuteGitTryCatch("GetMainBranchName", gitArgs, out gitOutput);
			if ( gitOutput.Count == 0 ) {
				PrintGitHistory();
				ControlFlow.Assert(gitOutput.Count != 0, "GetMainBranchName failed to return any results...");
			}

			foreach ( LineOutColored output in gitOutput ) {
				if ( output.Text.Trim().StartsWith("HEAD branch: ") ) {
					// We found what is considered the main branch.  Retrieve it.
					MainBranchName = output.Text.Trim().Substring(13);
					return;
				}
			}

			stopwatch.Stop();
			ProcessTimeLogs.Add(new GitProcessLogTime("GetMainBranchName", gitArgs, stopwatch.ElapsedMilliseconds));
			ControlFlow.Assert(true == false, "Did not find the required output text from the git command to determine the main branch name");
		}



		/// <summary>
		/// Determines if there are any uncommitted changes on the current branch.
		/// </summary>
		/// <returns></returns>
		public void RefreshUncommittedChanges () {
			try {
				Stopwatch stopwatch = new();
				stopwatch.Start();

				string gitArgs = "update-index -q --refresh";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("RefreshUncommittedChanges::: Git Command failed:  git " + gitArgs);

				gitArgs = "diff-index --quiet HEAD --";
				if ( !ExecuteGit_NoOutput(gitArgs) )
					AreUncommitedChangesOnLocalBranch = true;
				else
					AreUncommitedChangesOnLocalBranch = false;

				stopwatch.Stop();
				ProcessTimeLogs.Add(new GitProcessLogTime("RefreshUncommittedChanges", "Multiple Git Calls", stopwatch.ElapsedMilliseconds));
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw new ApplicationException("RefreshUncommittedChanges error.",e);
			}
		}




		/// <summary>
		/// Gets most recent tag for a given development "branch".  Note this is not an actual branch from repository, but rather one of (main, alpha, beta)
		/// that might be tagged with alpha, beta or nothing..
		/// </summary>
		/// <param name="branch"></param>
		/// <returns></returns>
		public SemVersion GetMostRecentVersionTagOfBranch (string branch) {
			List<LineOutColored> gitOutput;
			string gitArgs = "for-each-ref refs/tags/Ver*-" + branch + "* --count=1 --sort=-version:refname";
			ExecuteGitTryCatch("GetMostRecentVersionTagOfBranch", gitArgs, out gitOutput);
			if ( gitOutput.Count == 0 ) {
				return new SemVersion(0, 0, 0);
			}

			string outputRec = gitOutput [0].Text;
			char [] separators = new [] {' ', '\t'};
			string [] columns = outputRec.Split(separators);
			ControlFlow.Assert((columns.Length == 3), "Command output produced unexpected output. [" + outputRec + "]");
			string value = columns [2].Substring(10);
			ControlFlow.Assert(value.StartsWith("Ver"), "Did not find the Version tag marker:  Ver in the output record: [" + outputRec + "]");
			return ConvertVersionToSemVersion(value);
		}


		/// <summary>
		/// Performs a remote refresh to ensure local git repo branch is current with remote
		/// </summary>
		/// <param name="branchName"></param>
		/// <returns></returns>
		public bool RefreshLocalBranchStatus (string branchName = null) {
			Stopwatch stopwatch = new();
			stopwatch.Start();

			if ( branchName == null ) branchName = CurrentBranch;

			try {
				string gitArgs = "remote update";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("RefreshLocalBranchStatus::: Git Command failed:  git " + gitArgs);

				gitArgs = "status uno";
				if ( !ExecuteGit_NoOutput(gitArgs) )
					throw new ApplicationException("RefreshLocalBranchStatus::: Your local branch: [" +
					                               branchName +
					                               "] is not up to date with the remote one.  You will need to manually correct and then try again.");
				IsLocalBranchUptoDate = true;

				stopwatch.Stop();
				ProcessTimeLogs.Add(new GitProcessLogTime("RefreshLocalBranchStatus", gitArgs, stopwatch.ElapsedMilliseconds));
				return true;
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw;
			}
		}


		/// <summary>
		/// Commits all changes in current branch with the provided message.
		/// </summary>
		/// <param name="commitMsg"></param>
		public void CommitChanges (string commitMsg) {
			try
			{
				string commitErrStart = "CommitChanges:::  Git Command Failed:  git ";
				List<LineOutColored> gitOutput;
				string gitArgs = string.Format("commit --all --message=\"{0}\"",  commitMsg);
				if ( !ExecuteGit(gitArgs, out gitOutput) ) {
					if ( gitOutput.Count > 0 ) {
						if ( gitOutput [gitOutput.Count - 1].Text.Contains("nothing to commit, working tree clean") ) return;
					}
					else 
						throw new ApplicationException(commitErrStart + gitArgs);
				}
			}
			catch (Exception e)
			{
				PrintGitHistory();
				throw;
			}

		}


		/// <summary>
		/// Used to push the app into a Development commit, which means it is tagged with a SemVer tag, such as 2.5.6-alpha1001
		/// </summary>
		public void CommitSemVersionChanges (string tagVersion, string tagDescription) {
			try {
				//if ( WasVersionPreviouslyCommitted ) return;
				string commitErrStart = "CommitSemVersionChanges:::  Git Command Failed:  git ";

				string gitArgs = "";

				gitArgs = string.Format("tag -a {0} -m \"{1}\"", tagVersion, tagDescription);
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);

				gitArgs = "push --set-upstream origin " + CurrentBranch;
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);

				gitArgs = "push --tags origin";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException(commitErrStart + gitArgs);
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw;
			}
		}



		/// <summary>
		/// This is the Main Branch Commit stage
		/// </summary>
		public void CommitMainVersionChanges (string tagVersion, string tagDescription) {
			string gitArgs;
			List<LineOutColored> gitOutput;

			// This is not an update, it is a redo of a previous run that may have errored or its a clean run, but no changes have been committed.  So we skip this.
			if ( WasVersionPreviouslyCommitted ) return;

			try {
				// Checkout main and merge the current branch in
				if ( !IsCurrentBranchMainBranch() ) {
					gitArgs = "checkout " + MainBranchName;
					if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::  .Git Command failed:  git " + gitArgs);

					gitArgs = string.Format("merge {0} --no-ff --no-edit -m \"Merging Branch: {0}   |  {1}\"", CurrentBranch, CISession.VersionInfo.SemVersion);
					if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::  .Git Command failed:  git " + gitArgs);
				}


				gitArgs = "add .";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::  .Git Command failed:  git " + gitArgs);

				gitArgs = string.Format("commit -m \"{0} {1}", COMMIT_MARKER, tagDescription);
				if ( !ExecuteGit(gitArgs, out gitOutput) ) {
					if ( !gitOutput.Last().Text.Contains("nothing to commit") )
						throw new ApplicationException("CommitMainVersionChanges:::   .Git Command failed:  git " + gitArgs);
				}

				gitArgs = string.Format("tag -a {0} -m \"{1}\"", tagVersion, tagDescription);
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::   .Git Command failed:  git " + gitArgs);

				gitArgs = "push origin ";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::   .Git Command failed:  git " + gitArgs);

				gitArgs = "push --tags origin";
				if ( !ExecuteGit_NoOutput(gitArgs) ) throw new ApplicationException("CommitMainVersionChanges:::   .Git Command failed:  git " + gitArgs);

				// Delete the Feature Branch
				if ( CurrentBranch != MainBranchName ) {
					// See if branch exists on origin.  If not we expect an error below.
					gitArgs = "ls-remote --exit-code --heads origin " + CurrentBranch;
					bool bErrorIsExpected = false;
					if ( !ExecuteGit_NoOutput(gitArgs) ) bErrorIsExpected = true;

					gitArgs = "branch -d " + CurrentBranch;
					if ( !ExecuteGit_NoOutput(gitArgs) && !bErrorIsExpected ) {
						ControlFlow.AssertWarn(
							1 == 0,
							"Unable to delete the local branch [" +
							CurrentBranch +
							"see Git Errors below.  Will continue to process since this is not a fatal error.  You may need to perform branch cleanup manually.");
						PrintGitHistory();

						// We will probably fail the next error too...
						bErrorIsExpected = true;
					}

					gitArgs = "push origin --delete " + CurrentBranch;
					if ( !ExecuteGit_NoOutput(gitArgs) )
						if ( !bErrorIsExpected )
							throw new ApplicationException("CommitVersionChanges:::   .Git Command failed:  git " + gitArgs);
					Logger.Success("The previous 2 commits will issue errors if the local branch was never pushed to origin.  They can be safely ignored.");
				}
			}
			catch ( Exception e ) {
				PrintGitHistory();
				throw;
			}
		}


		/// <summary>
		/// Executes the Git Command, returning ONLY true or false to indicate success or failure
		/// </summary>
		/// <param name="cmdArguments"></param>
		/// <returns></returns>
		private bool ExecuteGit_NoOutput (string cmdArguments) {
			string command = GIT_COMMAND;
			HasErrored = false;

			// Log it
			LineOutColored output = LineOutColored.Normal(GIT_COMMAND_MARKER + command + " " + cmdArguments);
			GitCommandOutputHistory.Add(output);

			IProcess process = ProcessTasks.StartProcess(command, cmdArguments, CISession.RootDirectory,logInvocation: logInvocationLogging, logOutput: logOutputLogging); 
			process.AssertWaitForExit();

			// Copy output to history.
			GitCommandOutputHistory.AddRange(process.Output);


			if (process.ExitCode != 0)
			{
				HasErrored = true;
				return false;
			}

			return true;
		}


		/// <summary>
		/// Executes the requested Git Command AND returns the output.  Returns True on success, false otherwise
		/// </summary>
		/// <param name="cmdArguments"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		private bool ExecuteGit (string cmdArguments, out List<LineOutColored> output) {
			string command = GIT_COMMAND;
			HasErrored = false;

			// Log it
			LineOutColored outputCmd = LineOutColored.Normal(GIT_COMMAND_MARKER + command + " " + cmdArguments);

			GitCommandOutputHistory.Add(outputCmd);

			//ProcessTasks.DefaultLogOutput = false;
			IProcess process = ProcessTasks.StartProcess(command, cmdArguments, CISession.RootDirectory,logInvocation: logInvocationLogging, logOutput: logOutputLogging);

			//,customLogger:GitProcessorLoggerNormal
			process.AssertWaitForExit();
			output = process.Output.ToList();

			// Copy output to history.
			GitCommandOutputHistory.AddRange(process.Output);

			if ( process.ExitCode != 0 ) {
				HasErrored = true;
				return false;
			}
			return true;
		}


		/// <summary>
		/// Executes the given git command.  Returns the output in the output variable.  If the git command throws an error
		/// then the error is logged, the entire Git history is printed and the error is re-thrown.
		/// </summary>
		/// <param name="cmdName">Descriptive name for the command being run</param>
		/// <param name="cmdArguments">The arguments to pass to the git command</param>
		/// <param name="output">The output of the git command if successful</param>
		private void ExecuteGitTryCatch (string cmdName, string cmdArguments, out List<LineOutColored> output) {
			try {
				string gitArgs = cmdArguments;
				ControlFlow.Assert(ExecuteGit(gitArgs, out output) == true, cmdName + ":::  .Git Command Failed:  git " + gitArgs);
			}
			catch ( Exception e ) {
				HasErrored = true;
				PrintGitHistory();
				throw;
			}
		}
		

		/// <summary>
		/// Returns the number of commits on the current branch.
		/// </summary>
		/// <returns></returns>
		public int GetBranchCommitCount () {
			string gitArgs = string.Format("reflog show --no-abbrev {0}", CurrentBranch);
			if ( ExecuteGit(gitArgs, out List<LineOutColored> gitOutput) ) { return gitOutput.Count - 1; }

			ControlFlow.Assert(1 == 0, "Unable to determine how many commits are on current branch.");
			return 0;
		}

		/*
		 * This is unused, but there is potential that we might need it one day, I have left the code
		 * in on purpose...
		 *
		public List<Remotes> FetchRemotes () {
			
			throw new NotImplementedException();

			List<LineOutColored> gitOutput;
			string gitArgs = "remote -v";
			ExecuteGitTryCatch("FetchRemotes", gitArgs, out gitOutput);
			ControlFlow.Assert(gitOutput.Count > 1,
			                   "Git Remotes should always return at least 2 entries.  We only received [" + gitOutput.Count + "] entries.");
			if ( gitOutput.Count > 2 ) {
				Console.WriteLine();
				Console.WriteLine("This repository has more than 1 git remote identified.  You must select which one we will use for this repository",
				                  Color.Yellow);
				throw new NotImplementedException();
			}

			if ( gitOutput [0].Text.StartsWith("git version") ) { GitCommandVersion = gitOutput [0].Text.Substring(12); }
			else
				ControlFlow.Assert(true == false, "Retrieving Git --Version returned an invalid value of - " + gitOutput [0].Text);
		}
		*/


		/// <summary>
		/// Prints the history of the Git commands to the console.
		/// </summary>
		public void PrintGitHistory () {

			Console.WriteLine("");
			Console.WriteLine("Git Command Execution History is below for debugging purposes", Color.DeepSkyBlue);
			foreach ( LineOutColored line in GitCommandOutputHistory ) {
				if ( line.Text.StartsWith(GIT_COMMAND_MARKER) )
					Console.WriteLine("  " + line.Text.Substring(1), Color.Orange);
				else
					Console.WriteLine("     " + line.Text, Color.DarkKhaki);
			}
		}



		/// <summary>
		/// Retrieves information about a given commit, except for the differences information which can be very lengthy.
		/// </summary>
		/// <param name="commitHash"></param>
		/// <returns></returns>
		public List<LineOutColored> ShowCommitNoDiff (string commitHash) {
			List<LineOutColored> gitOutput;

			// git show --format=format:"M:slugci%ncommit: %h%nparents: %P%ncommitter: %cn%ncdate: %ct%nrefs: %d%nmsg: %s" 65a8e5
			string gitArgs = "show --no-patch --format=format:\"" +
			                 GIT_SHOW_SLUGCI +
			                 "%n" +
			                 GIT_SHOW_COMMIT +
			                 " %h" +
			                 "%n" +
			                 GIT_SHOW_PARENTS +
			                 " %P" +
			                 "%n" +
			                 GIT_SHOW_COMMITTER +
			                 " %cn" +
			                 "%n" +
			                 GIT_SHOW_COMMITTED_DATE +
			                 " %ct" +
			                 "%n" +
			                 GIT_SHOW_REFS +
			                 " %d" +
			                 "%n" +
			                 GIT_SHOW_MESSAGE +
			                 " %s" +
			                 "\" " +
			                 commitHash;

			ExecuteGitTryCatch("GitCatFile_OnCommit", gitArgs, out gitOutput);
			return gitOutput;
		}


		/// <summary>
		/// Lists branches which have been merged into the current branch or a descendant
		/// of the current branch.
		/// </summary>
		/// <param name="commitHash"></param>
		/// <returns></returns>
		public List<string> BranchMerged (string branchName, string commitHash) {
			List<LineOutColored> gitOutput;
			string gitArgs = "branch --merged " + commitHash;
			ExecuteGitTryCatch("BranchMerged", gitArgs, out gitOutput);

			
			List<string> merged = new List<string>();

			foreach ( LineOutColored output in gitOutput ) {
				if ( output.Text.StartsWith("*") ) continue;

				string branchFound = output.Text.Trim();
				if ( (branchFound != branchName) ) merged.Add(branchFound);
			}

			return merged;
		}


		/// <summary>
		/// Performs a Git Fetch of the given branch from the remote to the local repository WITHOUT doing a checkout!
		/// </summary>
		/// <param name="branchName">Name of branch to pull</param>
		/// <returns></returns>
		public bool FetchOriginToLocal (string branchName, bool rejectionIsValid = true) {
			if ( CurrentBranch == branchName ) return true;
			Stopwatch stopwatch = new();
			stopwatch.Start();

			List<LineOutColored> gitOutput;
			string gitArgs = "fetch origin " + branchName + ":" + branchName;

			if ( !ExecuteGit(gitArgs, out gitOutput) ) 
				// We may get a rejection, if this is acceptable we return true.
				if ( rejectionIsValid ) {
					if ( gitOutput.Count > 0 )
						if ( gitOutput [gitOutput.Count - 1].Text.Contains("! [rejected]") )
							return true;
					}
				else
					return false;

			stopwatch.Stop();
			ProcessTimeLogs.Add(new GitProcessLogTime("FetchOriginToLocal", gitArgs, stopwatch.ElapsedMilliseconds));
			return true;
		}


		/// <summary>
		/// Deletes a Branch
		/// <param name="local">If true, the local branch is deleted.  If false, the remote</param>
		/// </summary>
		/// <param name="branchName"></param>
		public bool DeleteBranch (string branchName, bool local = true) {
			List<LineOutColored> gitOutput;
			string gitArgs = "";
			if ( local == false )
				gitArgs = "push origin --delete " + branchName;
			else
				gitArgs = "branch -D " + branchName;

			bool success = ExecuteGit(gitArgs, out gitOutput);
			if ( !success ) {
				// See if it is an acceptable error
				foreach ( LineOutColored output in gitOutput ) {
					if ( output.Text.Contains("remote ref does not exist") ) return false;
				}

				ControlFlow.Assert(true == false, "DeleteBranch Failed: " + gitOutput [0].Text);
			}
			else
				return true;

			return true;
		}


		/// <summary>
		/// Fetches remote and removes any local branches that were removed from the remote
		/// </summary>
		public void FetchPrune () {
			List<LineOutColored> gitOutput;
			string gitArgs = "fetch -p";
			ExecuteGitTryCatch("FetchPrune", gitArgs, out gitOutput);
		}



		/// <summary>
		/// Custom logger for the GitProcessor
		/// </summary>
		/// <param name="type"></param>
		/// <param name="output"></param>
		internal static void CustomLogger (OutputType type, string output) {
			if ( type == OutputType.Err ) {
				Logger.Error(output);
				return;
			}

		}
	}
}