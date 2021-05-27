using System.Collections.Generic;
using JetBrains.Annotations;
using Nuke.Common;
using static Nuke.Common.IO.FileSystemTasks;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Semver;

namespace Slug.CI.NukeClasses
{
	/// <summary>
	/// Settings and other items related to the current CI Session
	/// </summary>
	public class CISession {
		private AbsolutePath _rootDirectory;


		/// <summary>
		/// The solution projects Main folder.
		/// </summary>
		public AbsolutePath RootDirectory {
			get { return _rootDirectory; }
			set {
				// Make sure folder exists
				ControlFlow.Assert(DirectoryExists(value),"The Root Directory does not exist.");
				_rootDirectory = (AbsolutePath)value;
			}
		}


		/// <summary>
		/// The RootCI folder
		/// </summary>
		public AbsolutePath SlugCIPath { get; set; }

		/// <summary>
		/// Full path and name to the SlugCI config file
		/// </summary>
		public AbsolutePath SlugCIFileName { get; set; }

		/// <summary>
		/// The location of the Src folder
		/// </summary>
		public AbsolutePath SourceDirectory { get; set; }

		/// <summary>
		/// The location of the Tests folder
		/// </summary>
		public AbsolutePath TestsDirectory { get; set; }

		/// <summary>
		/// The location of the Artifacts or Output folder
		/// </summary>
		public AbsolutePath OutputDirectory { get; set; }


		/// <summary>
		/// Where we are publishing to.
		/// </summary>
		public PublishTargetEnum PublishTarget { get; set; }

		
		/// <summary>
		/// The Compilation configuration to use
		/// </summary>
		public string CompileConfig { get; set; }



		/// <summary>
		/// Where coverage reports are located
		/// </summary>
		public AbsolutePath CoveragePath { get;  set; }


		/// <summary>
		/// Test output location
		/// </summary>
		public AbsolutePath TestOutputPath { get; set; }


		/// <summary>
		/// Path of the folder containing the .sln file.
		/// </summary>
		public AbsolutePath SolutionPath { get; set; }


		/// <summary>
		/// Full path and file name of the solution
		/// </summary>
		public string SolutionFileName { get; set; }

		/// <summary>
		/// The Solution we are processing
		/// </summary>
		public Solution Solution { get; set; }


		/// <summary>
		/// An instance of the GitProcessor
		/// </summary>
		public GitProcessor GitProcessor { get; set; }


		/// <summary>
		/// The SlugCIConfig object that contains numerous settings and project info
		/// </summary>
		public SlugCIConfig SlugCIConfigObj { get; set; }

		/// <summary>
		/// If true the SlugCIConfig file is not validated or updated with the latest changes to both SlugCI Config changes as well as solution changes to projects, such as add or deletes..
		/// <para>Generally only used for testing purposes to provide a slight speed increase when starting</para>
		/// </summary>
		public bool IsFastStart { get; set; }


		/// <summary>
		/// True if this is a user initiated run and the user can respond to keyboard input
		/// </summary>
		public bool IsInteractiveRun { get; set; }


		/// <summary>
		///  Level of verbosity for git Output
		/// </summary>
		public ProcessVerbosity VerbosityGitVersion { get; set; } = ProcessVerbosity.Nothing;


		/// <summary>
		/// Level of verbosity for the Compile Stage
		/// </summary>
		public DotNetVerbosity VerbosityCompile { get; set; }

		/// <summary>
		/// Level of verbosity for the Pack Stage
		/// </summary>
		public DotNetVerbosity VerbosityPack { get; set; }

		/// <summary>
		/// Level of verbosity for the CalcVersion Stage
		/// </summary>
		public Verbosity VerbosityCalcVersion { get; set; }


		/// <summary>
		/// The URL where Nuget Deploy targets are to be pushed to.
		/// </summary>
		public string NugetRepoURL { get; set; }


		/// <summary>
		/// The API key that grants access to read/write to the nuget repository.
		/// </summary>
		public string NugetAPIKey { get; set; }


		/// <summary>
		/// The standard log level that should be used for each Stage Execution.
		/// </summary>
		public LogLevel LoggingLevel { get; set; } = LogLevel.Normal;


		/// <summary>
		/// If true, Nuget Deploys will not actually publish to the Nuget URL.  The package will still be built, but it will not be sent to Nuget or local Repository.
		/// </summary>
		public bool SkipNuget { get; set; } 


		/// <summary>
		/// The number of projects whose deploy target is set to Nuget
		/// </summary>
		public short CountOfDeployTargetsNuget { get; set; }


		/// <summary>
		/// The number of projects whose deploy target is set to Copy
		/// </summary>
		public short CountOfDeployTargetsCopy { get; set; }


		/// <summary>
		/// Contains the results of the Publish Stage for each project
		/// </summary>
		public List<PublishResultRecord> PublishResults = new List<PublishResultRecord>();


		/// <summary>
		/// Environment variables we need.
		/// </summary>
		public Dictionary<string, string> EnvironmentVariables = new Dictionary<string, string>();


		/// <summary>
		/// List of all the stages run and their statistics and status
		/// </summary>
		public List<BuildStage> StageStats { get; private set; } = new List<BuildStage>();


		/// <summary>
		/// Where the root of projects that need to be deployed is
		/// </summary>
		public AbsolutePath DeployCopyPath {
			get; set;
		}

			
		/// <summary>
		/// The semantic version that will be set during this run!
		/// </summary>
		public SemVersion SemVersion { get; set; }


		/// <summary>
		/// Indicates the branch has had no changes to it, so process, but do not bump version
		/// or commit any changes...
		/// </summary>
		public bool WasPreviouslyCommitted { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public CISession () {
		}

	}

}
