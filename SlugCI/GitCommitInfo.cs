using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;
using Semver;

namespace Slug.CI
{
	public class GitCommitInfo {
		/// <summary>
		/// The Commit Hash
		/// </summary>
		public string CommitHash { get; private set; }
		
		/// <summary>
		/// The Commit Message
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// Who made the commit
		/// </summary>
		public string Committer { get; private set; }

		/// <summary>
		/// DateTime the commit was made
		/// </summary>
		public DateTime DateCommitted { get; private set; }

		/// <summary>
		/// A list of Tags associated with this commit
		/// </summary>
		public List<string> Tags { get; private set; } = new List<string>();


		/// <summary>
		/// Adds a tag to the list of Tags.
		/// </summary>
		/// <param name="tag"></param>
		public void AddTag (string tag) { Tags.Add(tag);}


		/// <summary>
		/// A list of branchess associated with this commit
		/// </summary>
		public List<string> Branches { get; private set; } = new List<string>();


		/// <summary>
		/// All of the Parent commit hashes
		/// </summary>
		public List<string> ParentCommits { get; private set; } = new List<string>();


		/// <summary>
		/// Returns true if this commit object has all commit info with it.  False if it is of the
		/// short format which does not have parent info.
		/// </summary>
		public bool IsFullCommitInfo { get; private set; }


		/// <summary>
		/// Returns the highest SemVersion tag found of all Verstion tags attached to this commit.  If none it returns 0.0.0
		/// </summary>
		/// <returns></returns>
		public SemVersion GetGreatestVersionTag () {
			List<SemVersion> versions = new List<SemVersion>();

			foreach ( string tag in Tags ) {
				if (tag.StartsWith("Ver"))
					if ( SemVersion.TryParse(tag.Substring(3), out SemVersion semVersion) ) {
					versions.Add(semVersion);
				}
			}

			if ( versions.Count == 1 ) return versions [0];

			SemVersion latestVersion = new SemVersion(0, 0, 0);
			foreach ( SemVersion semVersion in versions ) {
				if ( semVersion > latestVersion ) latestVersion = semVersion;
			}

			return latestVersion;
		}


		/// <summary>
		/// Returns true if the commit is a part of the branch specified. If IncludeOrigin is true, then origin/[branch] is also
		/// considered a match.
		/// </summary>
		/// <param name="branchName">name of branch to search for</param>
		/// <param name="includeOrigin">If true, origin/branchname is also considered same as just branchname</param>
		/// <returns></returns>
		public bool IsMemberOfBranch (string branchName, bool includeOrigin = true) {
			string origin = "origin/" + branchName;
			foreach ( string branch in Branches ) {
				if ( branch == branchName ) return true;
				if ( branch == origin ) return true;
			}

			return false;
		}



		/// <summary>
		/// Adds a branch to the branches list
		/// </summary>
		/// <param name="branch"></param>
		public void AddBranch (string branch) { Branches.Add(branch);}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hash"></param>
		public GitCommitInfo (string hash, string message, DateTime dateCommitted, string committer) {
			CommitHash = hash;
			Message = message;
			DateCommitted = dateCommitted;
			Committer = committer;
		}


		/// <summary>
		/// Constructor - Results in Short Output
		/// </summary>
		/// <param name="gitOutput"> The results of a commit show on one line</param>
		public GitCommitInfo (string gitOutput)
		{
			string[] results = gitOutput.Split('|');

			CommitHash = results [0];
			Committer = results [1];
			Message = results [3];

			// Convert Unix Time to DateTime
			DateCommitted = ConvertUnixTimeToDateTime(results [2]);
			/*
			if (!long.TryParse(results[2], out long value)) 
				ControlFlow.Assert(true == false, "The Git Commit date was unable to be converted to a Unix long time [" + results[2] + "]");
			//DateCommitted = DateTimeOffset.FromUnixTimeSeconds(value);
			DateTimeOffset d1 = DateTimeOffset.FromUnixTimeSeconds(value);
			DateCommitted = d1.LocalDateTime;
			*/
			// We always go to the last index of the string array for the last part.  This is due to the fact that some legacy
			// slugnuke commit messages had a | in them.  This causes the index count to be off.

			ProcessRefs(results[results.Length - 1]);
		}


		/// <summary>
		/// Converts Git Unix Date in Unix string format to DateTime
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private DateTime ConvertUnixTimeToDateTime (string value) {
			if (!long.TryParse(value, out long valueLong))
				ControlFlow.Assert(true == false, "The Git Commit date was unable to be converted to a Unix long time [" + value + "]");
			//DateCommitted = DateTimeOffset.FromUnixTimeSeconds(value);
			DateTimeOffset d1 = DateTimeOffset.FromUnixTimeSeconds(valueLong);
			DateTime d2= d1.LocalDateTime;
			return d2;
		}


		/// <summary>
		/// Constructor - From the output of a git show with full information
		/// </summary>
		/// <param name="output"></param>
		public GitCommitInfo (List<Output> outputLines) {
			// First, confirm we have the right output/
			ControlFlow.Assert(outputLines[0].Text.StartsWith(GitProcessor.GIT_SHOW_SLUGCI), "The output provided is not a SlugCI Commit Output structure!");
			outputLines.RemoveAt(0);

			foreach ( Output line in outputLines ) {
				string lineOut = line.Text;
				if ( line.Text.StartsWith(GitProcessor.GIT_SHOW_COMMIT) ) CommitHash = line.Text.Substring(GitProcessor.GIT_SHOW_COMMIT.Length+1);
				else if ( line.Text.StartsWith(GitProcessor.GIT_SHOW_PARENTS) ) {
					string [] parents = line.Text.Substring(GitProcessor.GIT_SHOW_PARENTS.Length + 1).Split(' ');
					foreach ( string parent in parents ) {
						ParentCommits.Add(parent);
					}
				}
				else if ( line.Text.StartsWith(GitProcessor.GIT_SHOW_COMMITTED_DATE) ) {
					DateCommitted = ConvertUnixTimeToDateTime(line.Text.Substring(GitProcessor.GIT_SHOW_COMMITTED_DATE.Length + 1));
				}
				else if ( line.Text.StartsWith(GitProcessor.GIT_SHOW_COMMITTER) ) Committer = line.Text.Substring(GitProcessor.GIT_SHOW_COMMITTER.Length + 1);
				else if ( line.Text.StartsWith(GitProcessor.GIT_SHOW_MESSAGE) ) Message = line.Text.Substring(GitProcessor.GIT_SHOW_MESSAGE.Length + 1);
				else if (line.Text.StartsWith(GitProcessor.GIT_SHOW_REFS))
					ProcessRefs(line.Text.Substring(GitProcessor.GIT_SHOW_REFS.Length + 1));
			}

			IsFullCommitInfo = true;
		}


		/// <summary>
		/// Interpret the Refs section into individual elements
		/// </summary>
		/// <param name="refArg"></param>
		private void ProcessRefs (string refArg) {
			// Convert the last field in output into multiple fields
			int first = refArg.IndexOf('(');
			ControlFlow.Assert(first > -1, "Unable to locate the property results starting character - (");
			int last = refArg.IndexOf(')');
			ControlFlow.Assert(last > -1, "Unable to locate the property results ending character - )");
			string props = refArg.Substring(++first, last - first);
			string[] properties = props.Split(',');


			// Now loop thru the properties for common elements that we further split out.
			foreach (string property in properties)
			{
				string propStr = property.Trim();
				if (propStr.StartsWith("tag:"))
					Tags.Add(propStr.Substring(5));
				else
					Branches.Add(propStr);
			}
		}


		/// <summary>
		/// ToString override
		/// </summary>
		/// <returns></returns>
		public override string ToString () { return CommitHash + " | " + DateCommitted + " |  " + Message; }
	}
}
