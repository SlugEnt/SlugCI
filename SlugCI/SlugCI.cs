using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities;
using Slug.CI.NukeClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CmdProcessor;
using Nuke.Common.Tooling;
using Semver;
using Slug.CI.SlugBuildStages;
using Console = Colorful.Console;


namespace Slug.CI
{
	/// <summary>
	/// This is the main processing logic for SlugCI.
	/// </summary>
	public class SlugCI {
		public const string SLUG_CI_CONFIG_FILE = "SlugCI_Config.json";
		public const string ENV_SLUGCI_DEPLOY_PROD = "SLUGCI_DEPLOY_PROD";
		public const string ENV_SLUGCI_DEPLOY_BETA = "SLUGCI_DEPLOY_BETA";
		public const string ENV_SLUGCI_DEPLOY_ALPHA = "SLUGCI_DEPLOY_ALPHA";
		public const string ENV_SLUGCI_DEPLOY_DEV = "SLUGCI_DEPLOY_DEV";
		public const string ENV_NUGET_REPO_URL = "NugetRepoUrl";
		public const string ENV_NUGET_API_KEY = "NugetApiKey";

		/// <summary>
		/// The CI Session information 
		/// </summary>
		public CISession CISession { get; private set; }



		/// <summary>
		/// Once Startup has been run this is true.
		/// </summary>
		public bool IsReady { get; set; }


		/// <summary>
		/// Output produced during the startup sequence
		/// </summary>
		private List<LineOutColored> LineOutput { get; set; } = new List<LineOutColored>();
		
		private LineOut NewLine { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession">The SlugCI Session object</param>
		public SlugCI (CISession ciSession) {
			LineOutput.Add(LineOutColored.NewLine());
			Color lineColor = Color.WhiteSmoke;

			CISession = ciSession;
			CISession.SlugCIPath = CISession.RootDirectory / ".slugci";
			CISession.SourceDirectory = CISession.RootDirectory / "src";
			CISession.TestsDirectory = CISession.RootDirectory / "tests";
			CISession.OutputDirectory = CISession.RootDirectory / "artifacts";
			CISession.SlugCIFileName = CISession.SlugCIPath / SLUG_CI_CONFIG_FILE;
			CISession.AngularDirectory = CISession.RootDirectory / "angular";
			CISession.CoveragePath = CISession.OutputDirectory / "Coverage";
			CISession.TestOutputPath = CISession.OutputDirectory / "Tests";

			ciSession.GitProcessor = new GitProcessor(ciSession);
		}


		private async Task PreLoadSolutionAsync () {
			
			foreach ( Project x in CISession.Solution.AllProjects ) {
				// Get Framework(s)
				string framework = x.GetProperty("TargetFramework");
			}
		}


		/// <summary>
		/// Performs initial steps:  Load slugciconfig, load projects, etc.
		/// </summary>
		public async Task StartupAsync () {
			// Get SlugCI Version
			string tempVersion = GetType().Assembly.GetName().Version.ToString();
			string [] versionBreaks = tempVersion.Split('.');
			CISession.SlugCI_Version = versionBreaks [0] + "." + versionBreaks [1] + "." + versionBreaks [2];
			
			
			// Run Some intialization checks and setup
			PreStage_Initialization preStageInitialization = new PreStage_Initialization(CISession);
			if ( !preStageInitialization.Execute() ) return;

			Task gitProcessTask = null;
			if (!CISession.IsInSetupMode) 
				gitProcessTask = Task.Run(CISession.GitProcessor.Startup);

			// Locate Solution and set solution related variables
			LoadSolutionInfo();


			// If We are in setup mode, then perform initial or update steps on the Config file.
			if ( CISession.IsInSetupMode ) {
				PreStage_ConvertToSlugCI converter = new PreStage_ConvertToSlugCI(CISession);
				converter.Execute();
				if (!converter.IsInSlugCIFormat)
				{
					ControlFlow.Assert(converter.IsInSlugCIFormat, "The solution is not in the proper SlugCI format.  This should be something that is automatically done.  Obviously something went wrong.");
					CISession.GitProcessor.RefreshUncommittedChanges();
				}

				// Reload solution info if it was moved.
				if (converter.SolutionWasMoved)
					LoadSolutionInfo();

				Console.WriteLine("Setup is complete.  Upon this apps exit, open the project in Visual Studio and ensure it all still opens and compiles.  Test projects are notorious for missing the source projects.",Color.Yellow);
				Console.WriteLine("Then restart SlugCI tool!",Color.Yellow);
				return;
			}


			//Task slnPreLoadTask = Task.Run(PreLoadSolutionAsync);



			//slnPreLoadTask.Wait();

			// Ensure Solution is in SlugCI format. If not migrate it.




			// Combine the Visual Studio solution project info into the individual projects of the
			// SlugCIConfig projects for easier access later on.
			//slnPreLoadTask.Wait();
			MergeVSProjectIntoSlugCI();


			// Store a shortcut to the projects right in CISession
			CISession.Projects = CISession.SlugCIConfigObj.Projects;


			// Setup Publish Results table 
			foreach (SlugCIProject project in CISession.Projects)
			{
				PublishResultRecord resultRecord = new PublishResultRecord(project);
				project.Results = resultRecord;
				CISession.PublishResults.Add(project.Name, resultRecord);
			}

			// Setup Publish Results for Angular Projects
			foreach ( AngularProject angularProject in CISession.SlugCIConfigObj.AngularProjects ) {
				PublishResultRecord resultRecord = new PublishResultRecord(angularProject);
				angularProject.Results = resultRecord;
				CISession.PublishResults.Add(angularProject.Name, resultRecord);
			}

			// Quick stats on number of Deploy targets by type
			short deployNone = 0;
			foreach (SlugCIProject slugCiProject in CISession.SlugCIConfigObj.Projects)
			{
				if (slugCiProject.Deploy == SlugCIDeployMethod.Copy) CISession.CountOfDeployTargetsCopy++;
				else if (slugCiProject.Deploy == SlugCIDeployMethod.Nuget || slugCiProject.Deploy == SlugCIDeployMethod.Tool)
					CISession.CountOfDeployTargetsNuget++;
				else
					deployNone++;
			}



			gitProcessTask.Wait();

			Task gbiTask = GetBranchInfoAsync();
			gbiTask.Wait();

			LineOutput.Add(new LineOutColored(EnumProcessOutputType.Std,"Git Command Version:  " + CISession.GitProcessor.GitCommandVersion,Color.Yellow));

			IsReady = true;
		}


		/// <summary>
		/// Retrieves information about all the branches (local and remote).  It removes
		/// remote branches that are the exact same as local CISession.GitBranches.  At this point in the
		/// process this should result in all remote branches that have local counterparts
		/// being removed from our processing lists.
		/// </summary>
		private async Task GetBranchInfoAsync()
		{
			List<RecordBranchLatestCommit> latestCommits;

			
			latestCommits = CISession.GitProcessor.GetAllBranchesWithLatestCommit();
			foreach (RecordBranchLatestCommit recordBranchLatestCommit in latestCommits)
			{
				GitBranchInfo branch = new GitBranchInfo(recordBranchLatestCommit, CISession.GitProcessor);
				CISession.GitBranches.Add(branch.Name, branch);
			}

			// Process the Dictionary and remove the remotes that are exact same as locals
			List<string> branchesToRemove = new List<string>();
			foreach (KeyValuePair<string, GitBranchInfo> branch in CISession.GitBranches)
			{
				if (branch.Value.Name.StartsWith("remotes/origin"))
				{
					string searchName = branch.Value.Name.Substring(15);
					if (CISession.GitBranches.ContainsKey(searchName))
					{
						GitBranchInfo b = CISession.GitBranches[searchName];
						if (branch.Value.IsSameAs(b, CISession.VerbosityCalcVersion)) branchesToRemove.Add(branch.Value.Name);
					}
				}
			}

			foreach (string s in branchesToRemove) { CISession.GitBranches.Remove(s); }
		}



		/// <summary>
		/// Writes all the lineout to the console.
		/// </summary>
		public void WriteLines ()
		{
			Misc.WriteMainHeader("SlugCI Initialization of Repository");
			
			foreach ( LineOutColored lineOut in LineOutput ) {
				lineOut.WriteToConsole();
			}
		}



		/// <summary>
		/// Displays information about the solution, its projects, git repo, etc.
		/// </summary>
		public void DisplayInfo () {
			List<string> info = new List<string>() {"SlugCI Version:  " + GetType().Assembly.GetName().Version.ToString()};

			Misc.WriteMainHeader(CISession.Solution.Name + "::  SlugCI / Repository Info",info);
			Console.Write("    {0,-25}","Project Root:",Color.WhiteSmoke);
			Console.WriteLine(CISession.RootDirectory,Color.Cyan);

			Console.Write("    {0,-25}", "Source Folder:", Color.WhiteSmoke);
			Console.WriteLine(CISession.SourceDirectory, Color.Cyan);

			Console.Write("    {0,-25}", "Tests Folder:", Color.WhiteSmoke);
			Console.WriteLine(CISession.TestsDirectory, Color.Cyan);

			Console.Write("    {0,-25}", "Output Folder:", Color.WhiteSmoke);
			Console.WriteLine(CISession.OutputDirectory, Color.Cyan);

			Console.Write("    {0,-25}", "Solution At:", Color.WhiteSmoke);
			Console.WriteLine(CISession.Solution.Path, Color.Cyan);

			Console.Write("    {0,-25}", "Nuget API Key:", Color.WhiteSmoke);
			Console.WriteLine(CISession.NugetAPIKey, Color.Cyan);

			Console.Write("    {0,-25}", "Nuget Repo URL:", Color.WhiteSmoke);
			Console.WriteLine(CISession.NugetRepoURL, Color.Cyan);

			Console.WriteLine();
			Console.WriteLine("-------------------------------------------------------------",Color.DarkCyan);
			Console.WriteLine("                  GIT  Information", Color.DarkCyan);
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);

			Console.Write("    {0,-25}", "Main Branch Name:", Color.WhiteSmoke);
			Console.WriteLine(CISession.GitProcessor.MainBranchName, Color.Cyan);

			Console.Write("    {0,-25}", "Current Branch Name:", Color.WhiteSmoke);
			Console.WriteLine(CISession.GitProcessor.CurrentBranch, Color.Cyan);


			Console.Write("    {0,-25}", "Git CommandLine Version:", Color.WhiteSmoke);
			Console.WriteLine(CISession.GitProcessor.GitCommandVersion, Color.Cyan);
			
			
			Console.WriteLine();
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine("                  Environment Variables", Color.DarkCyan);
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine("   Required: ", Color.Green);
			foreach ( KeyValuePair<string, string> envVar in CISession.EnvironmentVariables) {
				Console.Write("  {0,-35}  |  ",envVar.Key,Color.WhiteSmoke);
				Console.WriteLine("{0,-60}",  envVar.Value, Color.DarkCyan);
			}

			Console.WriteLine(Environment.NewLine + "  Missing Required:", Color.Red);
			foreach ( string envVar in CISession.MissingEnvironmentVariables ) {
				Console.WriteLine("  {0,-35}", envVar, Color.WhiteSmoke);
			}


			Console.WriteLine();
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine("                  Project Info", Color.DarkCyan);
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine(" {0,-60}{1,-10}   {2,-18}  {3,-30}","Project","How Deployed","Framework","Assembly Name",Color.Magenta);
			foreach ( SlugCIProject project in CISession.Projects ) {
				foreach ( string projectFramework in project.Frameworks ) {
					Console.WriteLine(" {0,-60}  {1,-10}   {2,-18}  {3,-30}", project.Name, project.Deploy.ToString(), projectFramework, project.AssemblyName, Color.WhiteSmoke);
				}
			}


			if ( CISession.IsInteractiveRun ) {
				while (Console.KeyAvailable) { Console.ReadKey(true); }
				Console.WriteLine("{0}Press Any key to continue",Environment.NewLine, Color.WhiteSmoke);
				Console.ReadKey();
			}
		}


		/// <summary>
		/// Returns the deployment folder for this run.  
		/// </summary>
		/// <returns></returns>
		private AbsolutePath GetDeployFolder () {
			bool useEnv = (CISession.SlugCIConfigObj.IsRootFolderUsingEnvironmentVariable(CISession.PublishTarget));

			if ( useEnv ) {
				string key = "";
				if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) key = ENV_SLUGCI_DEPLOY_ALPHA;
				else if ( CISession.PublishTarget == PublishTargetEnum.Production ) key = ENV_SLUGCI_DEPLOY_PROD;
				else if (CISession.PublishTarget == PublishTargetEnum.Beta) key = ENV_SLUGCI_DEPLOY_BETA;
				else if ( CISession.PublishTarget == PublishTargetEnum.Development ) key = ENV_SLUGCI_DEPLOY_DEV;
				else { ControlFlow.Assert(false == true, "Unable to determine the PublishTarget to get Deploy Environment Variable");}

				bool found = CISession.EnvironmentVariables.TryGetValue(key, out string value);
				if ( !found ) {
					Logger.Error("Environment variable [" + key + "] is required to be set due to this slugci config files setting to use environment variables for deploy target root paths.");
					return (AbsolutePath) null;
				}
				return (AbsolutePath) value;
			}

			if ( CISession.PublishTarget == PublishTargetEnum.Alpha ) return (AbsolutePath)CISession.SlugCIConfigObj.DeployAlphaRoot;
			if (CISession.PublishTarget == PublishTargetEnum.Beta) return (AbsolutePath)CISession.SlugCIConfigObj.DeployBetaRoot;
			if (CISession.PublishTarget == PublishTargetEnum.Production) return (AbsolutePath)CISession.SlugCIConfigObj.DeployProdRoot;
			if (CISession.PublishTarget == PublishTargetEnum.Development) return (AbsolutePath)CISession.SlugCIConfigObj.DeployDevRoot;

			return null;
		}

		private void LoadSolutionInfo () {
			List<string> solutionFiles = SearchForSolutionFile(CISession.RootDirectory.ToString(), ".sln");
			ControlFlow.Assert(solutionFiles.Count != 0, "Unable to find the solution file");
			ControlFlow.Assert(solutionFiles.Count == 1, "Found more than 1 solution file under the root directory -  - We can only work with 1 solution file." + CISession.RootDirectory.ToString());
			CISession.SolutionFileName = solutionFiles[0];
			CISession.Solution = SolutionSerializer.DeserializeFromFile<Solution>(CISession.SolutionFileName);
			CISession.SolutionPath = CISession.Solution.Directory;
		}


		/// <summary>
		/// Adds the Visual Studio project to the related SlugCIProject to make later looping thru projects easier...
		/// </summary>
		private void MergeVSProjectIntoSlugCI ()
		{
			foreach (Nuke.Common.ProjectModel.Project x in CISession.Solution.AllProjects) {
				SlugCIProject slugCIProject = CISession.SlugCIConfigObj.GetProjectByName(x.Name);
				if (slugCIProject == null) throw new ApplicationException("Trying to match SlugCIConfig Projects with Visual Studio Solution Projects failed.  This is unexpected... Visual Studio Project = [" + x.Name + "]");
				slugCIProject.VSProject = x;

				// Get Framework(s)
				string framework = x.GetProperty("TargetFramework");
				if (framework != null)  slugCIProject.Frameworks.Add(framework);
				else {
					framework = x.GetProperty("TargetFrameworks");
					if (framework != null) slugCIProject.Frameworks.AddRange(framework.Split(";"));
				}
				


				slugCIProject.AssemblyName = x.GetProperty("AssemblyName");
				slugCIProject.PackageId = x.GetProperty("PackageId");

				ControlFlow.Assert(!slugCIProject.AssemblyName.IsNullOrEmpty(),
				                   "Unable to locate the Assembly name from the .csproj for project [" + slugCIProject.Name + "]");
			}

		}




		/// <summary>
		/// Runs the SlugCI Process
		/// </summary>
		public void Execute () {
			if (CISession.GitProcessor.AreUncommitedChangesOnLocalBranch)
				throw new ApplicationException("There are uncommitted changes on the current branch: " + CISession.GitProcessor.CurrentBranch + "  Commit or discard existing changes and then try again.");

			// If we have Deploy "Copy" targets then make sure the variables are set and folders are valid
			if (CISession.CountOfDeployTargetsCopy > 0)
			{
				CISession.DeployCopyPath = GetDeployFolder();
				if (CISession.DeployCopyPath == null)
				{
					if (CISession.SlugCIConfigObj.IsRootFolderUsingEnvironmentVariable(CISession.PublishTarget))
						ControlFlow.Assert(CISession.DeployCopyPath != null,
						                   "The deploy folder for Copy Targets is null.  The slugci config file says to use environment variables to determine this setting.....");
					else
						ControlFlow.Assert(CISession.DeployCopyPath != null, "The deploy folder specified in slugci config file is empty..");
				}
			}


			SlugBuilder slugBuilder = new SlugBuilder(CISession);
		}




		/// <summary>
		/// Looks for the .sln file in the current folder and all subdirectories.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		public static List<string> SearchForSolutionFile(string root, string searchTerm)
		{
			List<string> files = new List<string>();

			foreach (var file in Directory.EnumerateFiles(root).Where(m => m.EndsWith(searchTerm)))
			{
				files.Add(file);
			}
			foreach (var subDir in Directory.EnumerateDirectories(root))
			{
				try
				{
					files.AddRange(SearchForSolutionFile(subDir, searchTerm));
				}
				catch (UnauthorizedAccessException)
				{
					// ...
				}
			}

			return files;
		}



		/// <summary>
		/// Sets the next version manually.
		/// </summary>
		/// <param name="userVersionInput"></param>
		/// <returns></returns>
		public bool SetVersionManually (string userVersionInput) {
			// Try to conver to a SemVer
			if ( !SemVersion.TryParse(userVersionInput, out SemVersion semVersion, true) ) return false;

			CISession.ManuallySetVersion = semVersion;
			return true;
		}


	}
}
