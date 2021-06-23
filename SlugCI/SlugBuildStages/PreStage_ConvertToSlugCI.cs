using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Console = Colorful.Console;

namespace Slug.CI.SlugBuildStages
{
	public class PreStage_ConvertToSlugCI : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public PreStage_ConvertToSlugCI (CISession ciSession) : base(BuildStageStatic.PRESTAGE_CONVERT_TO_SLUGCI, ciSession)
		{
		// Set expected directories.
			ExpectedSolutionPath = CISession.SourceDirectory;
			
			DotNetPath = ToolPathResolver.GetPathExecutable("dotnet");

			if (ciSession.IsFastStart)
			{
				// TODO - Need to restore this at some point, but needs to write to a AddOutputStage instead of screen. It interferes with prompting.
				// Misc.WriteSubHeader("FastStart:  Skipping normal checks and validations");
			}

		}


		/// <summary>
		/// Perform The Conversion
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			PreCheck();

			// Load Config file
			SlugCIConfig slugCiConfig = GetSlugCIConfig();
			if (slugCiConfig == null)
				throw new ApplicationException("The FastStart option was set, but there is not a valid SlugCIConfig file.  Either remove FastStart option or fix the problem.");
			IsInSlugCIFormat = true;

			return StageCompletionStatusEnum.Success;
		}

		
		/// <summary>
		/// True if the solution was moved as part of Converter process
		/// </summary>
		public bool SolutionWasMoved { get; private set; }


		internal AbsolutePath CurrentSolutionPath { get; set; }
		internal AbsolutePath ExpectedSolutionPath { get; set; }


		internal string DotNetPath { get; set; }


		private readonly List<VisualStudioProject> Projects = new List<VisualStudioProject>();


		/// <summary>
		/// True if the solution folder structure is setup for .Nuke / SlugNuke
		/// </summary>
		private bool IsSlugNukeFormat { get; set; }


		/// <summary>
		/// Returns true if the solution is in proper SluCI Format
		/// </summary>
		public bool IsInSlugCIFormat { get; set; }


		/// <summary>
		/// Returns the SlugCIConfig value
		/// </summary>
		private SlugCIConfig SlugCIConfig { get; set; }



		/// <summary>
		/// Performs some initial validations
		/// </summary>
		/// <returns></returns>
		public bool PreCheck()
		{
			// TODO - Uncomment this when using AddOutputText...in future...
			//Misc.WriteMainHeader("SlugCI: PreCheck",new List<string>() {"SlugCIConfig File"});


			// Check for old SlugNuke solution
			if (DirectoryExists(CISession.RootDirectory / ".nuke") || FileExists(CISession.RootDirectory / ".nuke"))
			{
				IsSlugNukeFormat = true;
				AOT_Warning("Detected previous SlugNuke Solution.  Will convert to SlugCI!");

				Convert_FromSlugNuke();
			}


			// Check for new SlugCI
			if (!FileSystemTasks.DirectoryExists(CISession.SlugCIPath))
				AOT_Warning(".SlugCI directory does not exist.  Proceeding with converting solution to .SlugCI format specifications");
			

			// Convert the project
			bool success = Converter();
			ControlFlow.Assert(success, "Failure during SlugCI Converter processing.");
			IsInSlugCIFormat = true;
			return true;
		}


		/// <summary>
		/// Either upgrades a current config or converts an existing solution into SlugCI format
		/// </summary>
		/// <returns></returns>
		public bool Converter()
		{
			// Create src folder if it does not exist.
			if (!DirectoryExists(CISession.SourceDirectory)) { Directory.CreateDirectory(CISession.SourceDirectory.ToString()); }

			// Create Tests folder if it does not exist.
			if (!DirectoryExists(CISession.TestsDirectory)) { Directory.CreateDirectory(CISession.TestsDirectory.ToString()); }

			// Create Artifacts / Output folder if it does not exist.
			if (!DirectoryExists(CISession.OutputDirectory)) { Directory.CreateDirectory(CISession.OutputDirectory.ToString()); }


			// Load an existing SlugCI file if there is one.
			SlugCIConfig = GetSlugCIConfig();


			// Load our own version of the Visual Studio project, so we can process some info from it
			foreach ( Project project in CISession.Solution.AllProjects ) {
				VisualStudioProject vsProject = GetInitProject(project);
				Projects.Add(vsProject);
			}


			// If New to SlugCI
			if ( SlugCIConfig == null ) {
				// Determine if any of these are a test project... If so we confirm with user and then move.
				foreach ( VisualStudioProject visualStudioProject in Projects ) {
					GetVisualStudioProjectInfoForNewProjectFromUser(visualStudioProject);
				}
				bool setupSuccess = SlugCI_NewSetup_EnsureProperDirectoryStructure(CISession.SolutionFileName);
				ControlFlow.Assert(setupSuccess, "Attempted to put solution in proper directory structure, but failed.");
			}
			else {
				// See if any new Projects, if so prompt for required info
				foreach ( VisualStudioProject visualStudioProject in Projects ) {
					SlugCIProject slugCIProject = CISession.SlugCIConfigObj.GetProjectByName(visualStudioProject.Name);
					if ( slugCIProject == null ) GetVisualStudioProjectInfoForNewProjectFromUser(visualStudioProject);
				}
			}

			// Ensure Config file is valid and up-to-date with current Class Structure
				ProcessSlugCIConfigFile();


			return (true);
		}



		/// <summary>
		/// Solicits information about a new project to the SlugCI config from the user (DeploymentMethod and whether it is a Test Project).
		/// </summary>
		/// <param name="project"></param>
		private void GetVisualStudioProjectInfoForNewProjectFromUser (VisualStudioProject visualStudioProject) {
			string lcprojName = visualStudioProject.Name.ToLower();
			if (lcprojName.StartsWith("test") || lcprojName.EndsWith("test"))
			{
				Console.WriteLine();
				Console.WriteLine(
					"The Project [ " +
					visualStudioProject.Name +
					" ]  Appears to be a test project.  If this is correct then it will be moved to the Tests Folder.  Is this correct (Y/N)?",
					Color.Yellow);
				while (true)
				{
					ConsoleKeyInfo key = Console.ReadKey();
					if (key.Key == ConsoleKey.Y)
					{
						
						visualStudioProject.IsTestProject = true;
						visualStudioProject.NewPath = CISession.TestsDirectory / visualStudioProject.Name;
						break;
					}

					if (key.Key == ConsoleKey.N) break;
				}
			}
			else
				visualStudioProject.SlugCIDeploymentMethod = PromptUserForDeployMethod(visualStudioProject);

		}


		/// <summary>
		/// Converts a project form SlugNuke to SlugCI
		/// </summary>
		public void Convert_FromSlugNuke () {
			// Move the config file to new name and location.  Delete no longer needed files.
			try
			{
				AbsolutePath oldNukeFile = CISession.RootDirectory / "nukeSolutionBuild.conf";
				EnsureExistingDirectory(CISession.SlugCIPath);
				if (!FileExists(CISession.SlugCIFileName))
					MoveFile(oldNukeFile, CISession.SlugCIFileName);
				else if (FileExists(oldNukeFile))
				{
					Logger.Warn(CISession.SlugCIFileName +
					            " already exists.  But so too does the old Config file.  Assuming this was from a prior error.  Removing the old file and LEAVING the current file intact.  Please check to ensure it is correct");
					DeleteFile(oldNukeFile);
				}

				// There could be either a .nuke folder or file - delete both
				DeleteFile(CISession.RootDirectory / ".nuke");
				DeleteDirectory(CISession.RootDirectory / ".nuke");
				DeleteFile(CISession.RootDirectory / "build.cmd");
				DeleteFile(CISession.RootDirectory / "build.ps1");
				DeleteFile(CISession.RootDirectory / "build.sh");
				DeleteFile(CISession.RootDirectory / "global.json");
				DeleteFile(CISession.RootDirectory / "gitversion.yml");
				IsSlugNukeFormat = false;
			}
			catch (Exception e)
			{
				AOT_Error("Failed to move the old SlugNuke Config file to new SlugCI file.");
				ControlFlow.Assert(true == false,"Error during SlugNuke conversion to SlugCI...");
			}
		}
		

		/// <summary>
		/// Returns the current SlugCIConfig object or loads it if null or force reload is true.
		/// </summary>
		/// <param name="forceReload"></param>
		/// <returns></returns>
		public SlugCIConfig GetSlugCIConfig(bool forceReload = false)
		{
			if (CISession.SlugCIConfigObj == null || forceReload == true)
			{
				CISession.SlugCIConfigObj  = SlugCIConfig = SlugCIConfig.LoadFromFile(CISession.SlugCIFileName);
			}

			return CISession.SlugCIConfigObj;
		}



		/// <summary>
		/// Adds a SlugCIProject that is based upon a Visual Studio Project
		/// </summary>
		/// <param name="vsProject"></param>
		private SlugCIProject AddSlugCIProject (SlugCIConfig slugCIConfig,VisualStudioProject vsProject) {

			SlugCIProject slugCIProject = new SlugCIProject() { Name = vsProject.Name };

			slugCIProject.IsTestProject = vsProject.IsTestProject;

			if (vsProject.IsTestProject)
			{
				slugCIProject.Deploy = SlugCIDeployMethod.None;

				// Also add the Required Nuget Coverage package
				CoverletInstall(vsProject);
			}
			else
			{
				slugCIProject.Deploy = vsProject.SlugCIDeploymentMethod;
			}
			slugCIConfig.Projects.Add(slugCIProject);
			return slugCIProject;
		}


		/// <summary>
		/// Ensures there is a valid SlugCI Config file and updates it if necessary OR creates it.
		/// </summary>
		/// <returns></returns>
		private bool ProcessSlugCIConfigFile() {
			SlugCIConfig slugCiConfig = SlugCIConfig;
			if (slugCiConfig == null)
			{
				EnsureExistingDirectory(CISession.SlugCIPath);
				slugCiConfig = new SlugCIConfig();
				slugCiConfig.DeployToVersionedFolder = true;
			}


			// Make a copy that we will compare against.
			SlugCIConfig origSlugCiConfig = slugCiConfig.Copy();

			// Ensure the version of the config file layout is set to most current.  We do this after the copy, so we can 
			// detect changes in the file layout, fields, etc.
			slugCiConfig.ConfigStructureVersion = CISession.SlugCI_Version;

			bool updateProjectAdd = false;
			bool hasCopyDeployMethod = false;


			// Now go thru the Visual Studio Projects and update the config
			foreach (VisualStudioProject project in Projects)
			{
				SlugCIProject slugCIProject = slugCiConfig.GetProjectByName(project.Name);

				// New Visual Studio project that does not exist in SlugCIConfig
				if ( slugCIProject == null ) {
					slugCIProject = AddSlugCIProject(slugCiConfig, project);
				}
				
				if (slugCIProject.Deploy == SlugCIDeployMethod.Copy) hasCopyDeployMethod = true;
			}


			// Ensure Deploy Roots have values if at least one of the projects has a deploy method of Copy
			if (hasCopyDeployMethod)
			{
				foreach (PublishTargetEnum value in Enum.GetValues(typeof(PublishTargetEnum)))
				{
					ValidateDeployFolders(value, slugCiConfig);
				}
			}


			// Add Angular Projects
			ProcessAngularProjects(slugCiConfig);


			// Determine if we need to save new config.
			if (origSlugCiConfig != slugCiConfig)
			{
				string json = JsonSerializer.Serialize<SlugCIConfig>(slugCiConfig, SlugCIConfig.SerializerOptions());
				File.WriteAllText(CISession.SlugCIFileName, json);
				Console.WriteLine("SlugCIConfig file updated to latest version / values", Color.Green);

				SlugCIConfig = GetSlugCIConfig(true);
				if (updateProjectAdd)
				{
					Logger.Warn("The file: {0} was updated.  One ore more projects were added.  Ensure they have the correct Deploy setting.", CISession.SlugCIFileName);
				}
			}

			return true;
		}



		/// <summary>
		/// Checks for Angular projects and ensures they exist in the SlugCIConfig.
		/// </summary>
		/// <param name="slugCiConfig"></param>
		private void ProcessAngularProjects(SlugCIConfig slugCiConfig)
		{
			if (!DirectoryExists(CISession.AngularDirectory))
			{
				Logger.Warn("No Angular directory found off root folder");
				return;
			}

			// Find all subdirectories - these are the angular projects.
			List<string> webProjects = Directory.EnumerateDirectories(CISession.AngularDirectory, "*.web").ToList();

			foreach (string webProject in webProjects)
			{
				AbsolutePath webFolder = (AbsolutePath)webProject;
				string name = Path.GetFileName(webFolder);
				// See if already part of config, if so, nothing to do.
				if (slugCiConfig.AngularProjects.Exists(a => a.Name == name)) continue;

				// Make sure it contains a package.json file.  If so we will assume it is a web project.
				AbsolutePath webFile = webFolder / "package.json";

				if (!FileExists(webFile))
				{
					Logger.Warn("Angular Folder does not contain a package.json file.  NOT ADDING TO AnuglarProjects - " + webFolder);
					continue;
				}

				slugCiConfig.AngularProjects.Add(new AngularProject(name));
			}

		}



		/// <summary>
		/// Prompts the user for the type of deployment the given project should use.
		/// </summary>
		/// <param name="projectName"></param>
		/// <returns></returns>
		private SlugCIDeployMethod PromptUserForDeployMethod(VisualStudioProject project)
		{
			Console.WriteLine();
			Console.WriteLine();
			AOT_Info("Project - [" + project.Name + "]  has been added to the SlugCI config file.");

			// Determine Deploy Method
			string deployMethod = "";
			if ( project.ProjectType == "Library" ) 
				deployMethod = "Nuget";
			else if ( project.ProjectType == "Exe" )
				deployMethod = "Copy";
			else
				deployMethod = "None";
			AOT_Normal("It appears the deployment method for this project would be " + deployMethod + ".  Press enter to accept this default or select letter.",Color.Yellow);


			//"What deployment method does this project use?", Color.Yellow);
			bool continuePrompting = true;
			while (continuePrompting)
			{
				Console.WriteLine("Press: ");
				Console.WriteLine("   (N) For Nuget Deploy");
				Console.WriteLine("   (C) For File Copy Deploy");
				Console.WriteLine("   (0) For Not Specified");
				while ( Console.KeyAvailable ) Console.ReadKey(false);
				ConsoleKeyInfo inputKey = Console.ReadKey();
				if ( inputKey.Key == ConsoleKey.Enter ) {
					if ( deployMethod == "Nuget" ) return SlugCIDeployMethod.Nuget;
					else if ( deployMethod == "Copy" )
						return SlugCIDeployMethod.Copy;
					else
						return SlugCIDeployMethod.None;
				}
				if (inputKey.Key == ConsoleKey.N) return SlugCIDeployMethod.Nuget;
				if (inputKey.Key == ConsoleKey.C) return SlugCIDeployMethod.Copy;
				if (inputKey.Key == ConsoleKey.D0) return SlugCIDeployMethod.None;
			}

			return SlugCIDeployMethod.None;
		}



		/// <summary>
		/// Ensures the Deploy Folders have a value and that it can be accessed.  If no value then it prompts user for value
		/// </summary>
		/// <param name="config"></param>
		/// <param name="slugCiConfig"></param>
		/// <returns></returns>
		private bool ValidateDeployFolders(PublishTargetEnum config, SlugCIConfig slugCiConfig)
		{
			// Does the config have a root folder set?
			if (!slugCiConfig.IsRootFolderSpecified(config))
			{
				string name = config.ToString();

				Misc.WriteSubHeader(name + ": Set Deploy Folder");
				Console.WriteLine("The [" + config + "] deployment folder is undefined in the SlugCI config file, You can enter a value OR press enter to force the system to look for and use environment variables.",
								  Color.DarkOrange);
				Console.WriteLine(
					"--: If you want to always use the environment variables for these entries, just hit the Enter key.  Otherwise enter a valid path to the root location they should be deployed too.",
					Color.DarkOrange);
				bool invalidAnswer = true;
				string answer = "";
				while (invalidAnswer)
				{
					Console.WriteLine();
					Console.WriteLine("Enter the root deployment folder for {0} [{1}]", Color.DarkCyan, name, config);
					answer = Console.ReadLine();
					answer = answer.Trim();
					if (answer == string.Empty) answer = "_";

					// Make sure such a folder exists.
					if (answer != "_")
					{
						if (DirectoryExists((AbsolutePath)answer)) invalidAnswer = false;
					}
					else invalidAnswer = false;
				}

				if (config == PublishTargetEnum.Production)
					slugCiConfig.DeployProdRoot = answer;
				else if (config == PublishTargetEnum.Alpha)
					slugCiConfig.DeployAlphaRoot = answer;
				else if (config == PublishTargetEnum.Beta)
					slugCiConfig.DeployBetaRoot = answer;
				else if (config == PublishTargetEnum.Development) slugCiConfig.DeployDevRoot = answer;
			}
			return true;
		}



		/// <summary>
		/// Ensure the Directory Structure is correct. Projects are in proper place.  
		/// </summary>
		/// <param name="solutionFile"></param>
		/// <returns></returns>
		private bool SlugCI_NewSetup_EnsureProperDirectoryStructure(string solutionFile)
		{
			bool solutionNeedsToMove = false;
			CurrentSolutionPath = CISession.Solution.Directory;
			if (CurrentSolutionPath.ToString() != ExpectedSolutionPath.ToString()) solutionNeedsToMove = true;

			// Query the solution for the projects that are in it.
			// We allow all tests to run, instead of failing at first failure.


			// There are 2 things we need to check.
			//  1.  Is solution in right folder?
			//  2.  Are projects in right folder.
			//  The Move process has to do the following:
			//   1. Move the project folder to proper place
			//   2. Remove the project from the solution
			//   3. Do steps 1, 2 for every project
			//   4. Move solution file to proper location
			//   5. Re-add all projects to solution

			try
			{
				List<VisualStudioProject> movedProjects = new List<VisualStudioProject>();
				foreach ( VisualStudioProject vsProject in Projects ) {
					if (vsProject.OriginalPath.ToString() != vsProject.NewPath.ToString() || solutionNeedsToMove ) {
						movedProjects.Add(vsProject);
						MoveProjectStepA(vsProject);
					}
				}


				// Step 4:  Is Solution in proper directory.  If not move it.
				if ( solutionNeedsToMove ) {
					string slnFileCurrent = CurrentSolutionPath / Path.GetFileName(solutionFile);
					string slnFileFuture = ExpectedSolutionPath / Path.GetFileName(solutionFile);
					File.Move(slnFileCurrent, slnFileFuture);
					SolutionWasMoved = true;
				}


				// Step 5.  Read project to solution
				if ( movedProjects.Count > 0 ) {
					foreach ( VisualStudioProject project in movedProjects ) { MoveProjectStepB(project); }
				}
			}
			catch ( Exception e ) {
				AOT_Error("An error occured during the project migration to SlugCI format.  Because of where this error occurred the project may be in an unusable state at this time.");
				AOT_Error("You can either revert all the changes in git, delete the entire project from this machine, re-clone it and start again, or attempt to fix it.");
				throw;
			}

			return true;
		}



		/// <summary>
		/// Moves a project of a solution:  Moves it's folder location to new location and then updates the solution.
		/// </summary>
		/// <param name="project">VisualStudioProject object representing the project to move.</param>
		/// <returns></returns>
		private bool MoveProjectStepA(VisualStudioProject project)
		{
			// Move project to new location
			if (project.OriginalPath.ToString() != project.NewPath.ToString())
				Directory.Move(project.OriginalPath, project.NewPath);

			// Remove from Solution
			string removeParam = Path.Combine(project.OriginalPath, project.Namecsproj);
			IProcess sln = ProcessTasks.StartProcess(DotNetPath, "sln " + CISession.SolutionFileName + " remove " + removeParam, logOutput: true);
			sln.AssertWaitForExit();
			ControlFlow.Assert(sln.ExitCode == 0, "Failed to remove Project: " + project.Name + " from solution so we could move it.");

			return true;
		}


		/// <summary>
		/// Adds the given project back to the solution
		/// </summary>
		/// <param name="project"></param>
		/// <returns></returns>
		private bool MoveProjectStepB(VisualStudioProject project)
		{
			// Now add it back to project with new location
			string addParam = Path.Combine(project.NewPath, project.Namecsproj);
			IProcess sln = ProcessTasks.StartProcess(DotNetPath, "sln " + ExpectedSolutionPath + " add " + addParam, logOutput: true);
			sln.AssertWaitForExit();
			ControlFlow.Assert(sln.ExitCode == 0, "Failed to re-add Project: " + project.Name + " to solution so we could complete the move");

			AOT_Success(String.Format("Project: {0} successfully relocated into proper new directory layout.", project.Name));
			return true;
		}


		/// <summary>
		/// Takes the current Project path and creates an official VisualStudioProject object from it.
		/// </summary>
		/// <param name="path">Path as returned from "dotnet sln" command</param>
		/// <returns></returns>
		public VisualStudioProject GetInitProject(Project nukeProject)
		{
			VisualStudioProject visualStudioProject = new VisualStudioProject(nukeProject);

			string lcprojName = visualStudioProject.Name.ToLower();

			AbsolutePath newRootPath = ExpectedSolutionPath;

			visualStudioProject.ProjectType = nukeProject.GetProperty("OutputType");

			visualStudioProject.OriginalPath = nukeProject.Directory;
			visualStudioProject.NewPath = newRootPath / Path.GetFileName(nukeProject.Directory);

			return visualStudioProject;
		}


		/// <summary>
		/// Installs Coverlet into Test Projects
		/// </summary>
		/// <param name="vsProject"></param>
		/// <returns></returns>
		private bool CoverletInstall(VisualStudioProject vsProject)
		{
			// Determine csproj path
			AbsolutePath csprojPath = vsProject.NewPath;
			DotNet("add package coverlet.msbuild --version 3.0.3", csprojPath);
			return true;
		}

	}
}
