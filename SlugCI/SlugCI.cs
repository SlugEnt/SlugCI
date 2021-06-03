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
		/// List of Environment Variables that are missing
		/// </summary>
		private List<string> MissingEnvironmentVariables { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession">The SlugCI Session object</param>
		public SlugCI (CISession ciSession) {
			Console.WriteLine(Environment.NewLine);
			Color lineColor = Color.WhiteSmoke;
			Console.ForegroundColor = lineColor;

			Misc.WriteMainHeader("SlugCI Initialization of Repository");


			CISession = ciSession;
			CISession.SlugCIPath = CISession.RootDirectory / ".slugci";
			CISession.SourceDirectory = CISession.RootDirectory / "src";
			CISession.TestsDirectory = CISession.RootDirectory / "tests";
			CISession.OutputDirectory = CISession.RootDirectory / "artifacts";
			CISession.SlugCIFileName = CISession.SlugCIPath / SLUG_CI_CONFIG_FILE;


			CISession.CoveragePath = CISession.OutputDirectory / "Coverage";
			CISession.TestOutputPath = CISession.OutputDirectory / "Tests";


			ciSession.GitProcessor = new GitProcessor(ciSession);
			if (ciSession.GitProcessor.AreUncommitedChangesOnLocalBranch) 
				throw new ApplicationException("There are uncommitted changes on the current branch: " + ciSession.GitProcessor.CurrentBranch +  "  Commit or discard existing changes and then try again.");


			CheckForEnvironmentVariables();

			// Locate Solution and set solution related variables
			LoadSolutionInfo();


			// Ensure Solution is in SlugCI format. If not migrate it.
			ConvertToSlugCI converter = new ConvertToSlugCI(CISession);
			if ( !converter.IsInSlugCIFormat ) {
				ControlFlow.Assert(converter.IsInSlugCIFormat,"The solution is not in the proper SlugCI format.  This should be something that is automatically done.  Obviously something went wrong.");
			}


			// Reload solution info if it was moved.
			if (converter.SolutionWasMoved) 
				LoadSolutionInfo();


			// Combine the Visual Studio solution project info into the individual projects of the
			// SlugCIConfig projects for easier access later on.
			MergeVSProjectIntoSlugCI();


			// Store a shortcut to the projects right in CISession
			CISession.Projects = CISession.SlugCIConfigObj.Projects;


			// Setup Publish Results table 
			foreach ( SlugCIProject project in ciSession.Projects ) {
				PublishResultRecord resultRecord = new PublishResultRecord(project);
				project.Results = resultRecord;
				ciSession.PublishResults.Add(project.Name,resultRecord);
			}

			// Quick stats on number of Deploy targets by type
			short deployNone = 0;
			foreach ( SlugCIProject slugCiProject in ciSession.SlugCIConfigObj.Projects ) {
				if ( slugCiProject.Deploy == SlugCIDeployMethod.Copy ) ciSession.CountOfDeployTargetsCopy++;
				else if ( slugCiProject.Deploy == SlugCIDeployMethod.Nuget )
					ciSession.CountOfDeployTargetsNuget++;
				else
					deployNone++;
			}


			// If we have Deploy "Copy" targets then make sure the variables are set and folders are valid
			if ( ciSession.CountOfDeployTargetsCopy > 0 ) {
				ciSession.DeployCopyPath = GetDeployFolder();
				if ( ciSession.DeployCopyPath == null ) {
					if ( ciSession.SlugCIConfigObj.IsRootFolderUsingEnvironmentVariable(ciSession.PublishTarget) )
						ControlFlow.Assert(ciSession.DeployCopyPath != null,
						                   "The deploy folder for Copy Targets is null.  The slugci config file says to use environment variables to determine this setting.....");
					else
						ControlFlow.Assert(ciSession.DeployCopyPath != null, "The deploy folder specified in slugci config file is empty..");
				}
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
			foreach ( string envVar in MissingEnvironmentVariables ) {
				Console.Write("  {0,-35}", envVar, Color.WhiteSmoke);
			}


			Console.WriteLine();
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine("                  Project Info", Color.DarkCyan);
			Console.WriteLine("-------------------------------------------------------------", Color.DarkCyan);
			Console.WriteLine(" {0,-29}{1,-10}   {2,-18}  {3,-30}","Project","How Deployed","Framework","Assembly Name",Color.Magenta);
			foreach ( SlugCIProject project in CISession.Projects ) {
				Console.WriteLine(" {0,-30}  {1,-10}  {2,-18}  {3,-30}", project.Name,project.Deploy.ToString(),project.Framework, project.AssemblyName, Color.WhiteSmoke);
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
			SlugBuilder slugBuilder = new SlugBuilder(CISession);
		}

		/// <summary>
		/// Checks to ensure environment variables are set.
		/// </summary>
		/// <returns></returns>
		private bool CheckForEnvironmentVariables()
		{
			List<string> requiredEnvironmentVariables  = new List<string>()
			{
				ENV_SLUGCI_DEPLOY_PROD,
				ENV_SLUGCI_DEPLOY_BETA,
				ENV_SLUGCI_DEPLOY_ALPHA,
				ENV_SLUGCI_DEPLOY_DEV,
				ENV_NUGET_REPO_URL,
				ENV_NUGET_API_KEY
			};

			MissingEnvironmentVariables = new List<string>();

			foreach (string name in requiredEnvironmentVariables)
			{
				string result = Environment.GetEnvironmentVariable(name);
				if (result == null) MissingEnvironmentVariables.Add(name);
				else {
					CISession.EnvironmentVariables.Add(name,result);

					// Load the Environment Variables
					switch ( name ) {
						case ENV_NUGET_REPO_URL: CISession.NugetRepoURL = result;
							break;
						case ENV_NUGET_API_KEY: CISession.NugetAPIKey = result;
							break;
					}
				}
			}

			if (MissingEnvironmentVariables.Count == 0)
			{
				Console.WriteLine("All required environment variables found", Color.Green);
				return true;
			}

			Console.WriteLine(Environment.NewLine + "Some environment variables are missing.  These may or may not be required.", Color.Yellow);
			foreach (string item in MissingEnvironmentVariables) Console.WriteLine("  -->  " + item,Color.Yellow);
			return false;
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
	}
}
