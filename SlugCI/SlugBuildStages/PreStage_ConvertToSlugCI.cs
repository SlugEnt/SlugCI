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
			}


			// Check for new SlugCI
			if (!FileSystemTasks.DirectoryExists(CISession.SlugCIPath))
				AOT_Warning(".SlugCI directory does not exist.  Proceeding with converting solution to .SlugCI format specifications");
			

			// Convert the project
			ControlFlow.Assert(Converter(), "Failure during SlugCI Converter processing.");
			IsInSlugCIFormat = true;
			return true;
		}


		/// <summary>
		/// Either upgrades a current config or converts an existing solution into SlugCI format
		/// </summary>
		/// <returns></returns>
		public bool Converter()
		{
			bool solutionConverted = false;
			if (CISession.SolutionFileName != null && CISession.SolutionFileName != string.Empty) solutionConverted = Convert_VisualStudioSolution();

			// Ensure Config file is valid and up-to-date with current Class Structure
			ProcessSlugCIConfigFile();


			return (solutionConverted);
		}


		/// <summary>
		/// Performs initial logic to ensure that the Solution is ready for the SlugCI build process.
		///   - Solution is in the proper directory structure
		///     - If not, it will move it to the proper structure
		///   - Ensure a .SlugCI file exists.
		/// </summary>
		/// <returns></returns>
		public bool Convert_VisualStudioSolution()
		{
			Logger.Normal("Solution File found:  {0}", CISession.SolutionFileName);

			// A.  Proper Directory Structure
			ControlFlow.Assert(ProperDirectoryStructure(CISession.SolutionFileName), "Attempted to put solution in proper directory structure, but failed.");


			// B.  We need to upgrade from SlugNuke if the SlugNuke config file was found.
			if (IsSlugNukeFormat)
			{
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
					Logger.Warn("Failed to move the old SlugNuke Config file to new SlugCI file.");
					Logger.Error(e);
				}
			}

			return true;
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

			return SlugCIConfig;
		}



		/// <summary>
		/// Ensures there is a valid SlugCI Config file and updates it if necessary OR creates it.
		/// </summary>
		/// <returns></returns>
		private bool ProcessSlugCIConfigFile()
		{
			SlugCIConfig slugCiConfig = GetSlugCIConfig();
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
				if (slugCIProject == null)
				{
					updateProjectAdd = true;
					slugCIProject = new SlugCIProject() { Name = project.Name };
					
					slugCIProject.IsTestProject = project.IsTestProject;

					if (project.IsTestProject)
					{
						slugCIProject.Deploy = SlugCIDeployMethod.None;

						// Also add the Required Nuget Coverage package
						CoverletInstall(project);
					}
					else
					{
						slugCIProject.Deploy = PromptUserForDeployMethod(project.Name);
					}
					slugCiConfig.Projects.Add(slugCIProject);
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
		private SlugCIDeployMethod PromptUserForDeployMethod(string projectName)
		{
			Console.WriteLine();
			Colorful.Console.WriteLine("Project - [" + projectName + "]  has been added to the SlugCI config file.  What deployment method does this project use?", Color.Yellow);
			bool continuePrompting = true;
			while (continuePrompting)
			{
				Console.WriteLine("Press: ");
				Console.WriteLine("   (N) For Nuget Deploy");
				Console.WriteLine("   (C) For File Copy Deploy");
				Console.WriteLine("   (0) For Not Specified");
				while ( Console.KeyAvailable ) Console.ReadKey(false);
				ConsoleKeyInfo inputKey = Console.ReadKey();
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
		private bool ProperDirectoryStructure(string solutionFile)
		{
			// Create src folder if it does not exist.
			if (!DirectoryExists(CISession.SourceDirectory)) { Directory.CreateDirectory(CISession.SourceDirectory.ToString()); }

			// Create Tests folder if it does not exist.
			if (!DirectoryExists(CISession.TestsDirectory)) { Directory.CreateDirectory(CISession.TestsDirectory.ToString()); }

			// Create Artifacts / Output folder if it does not exist.
			if (!DirectoryExists(CISession.OutputDirectory)) { Directory.CreateDirectory(CISession.OutputDirectory.ToString()); }

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
			bool solutionNeedsToMove = false;
			CurrentSolutionPath = CISession.Solution.Directory;
			if (CurrentSolutionPath.ToString() != ExpectedSolutionPath.ToString()) solutionNeedsToMove = true;

			try {
				List<VisualStudioProject> movedProjects = new List<VisualStudioProject>();
				foreach ( Project project in CISession.Solution.AllProjects ) {
					VisualStudioProject vsProject = GetInitProject(project);
					Projects.Add(vsProject);
					if ( (vsProject.OriginalPath.ToString() != vsProject.NewPath.ToString()) || solutionNeedsToMove ) {
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
		public VisualStudioProject GetInitProject(Nuke.Common.ProjectModel.Project nukeProject)
		{
			VisualStudioProject visualStudioProject = new VisualStudioProject(nukeProject);

			string lcprojName = visualStudioProject.Name.ToLower();

			AbsolutePath newRootPath = ExpectedSolutionPath;
			if (lcprojName.StartsWith("test") || lcprojName.EndsWith("test"))
			{
				Console.WriteLine("The Project [ " + nukeProject + " ]  Appears to be a test project.  If this is correct then it will be moved to the Tests Folder.  Is this correct (Y/N)?",Color.Yellow);
				while (true) {
					ConsoleKeyInfo key = Console.ReadKey();
					if ( key.Key == ConsoleKey.Y ) {
						visualStudioProject.IsTestProject = true;
						newRootPath = CISession.TestsDirectory;
						break;
					}

					if ( key.Key == ConsoleKey.N ) break;
				}
			}

			visualStudioProject.OriginalPath = nukeProject.Directory;
			visualStudioProject.NewPath = newRootPath / Path.GetFileName(nukeProject.Directory);

			return visualStudioProject;
		}



		/// <summary>
		/// Determines the Project's targeted framework.  It checks both FrameWork and Frameworks and throws the results into a list.
		/// </summary>
		/// <param name="project"></param>
		private void DetermineFramework(VisualStudioProject project)
		{
			// Determine csproj path
			AbsolutePath csprojPath = project.OriginalPath / project.Namecsproj;


			XDocument doc = XDocument.Load(csprojPath);
			string value = "";
			if (doc.XPathSelectElement("//PropertyGroup/TargetFramework") != null)
				value = doc.XPathSelectElement("//PropertyGroup/TargetFramework").Value;
			else if (doc.XPathSelectElement("//PropertyGroup/TargetFrameworks") != null)
				value = doc.XPathSelectElement("//PropertyGroup/TargetFrameworks").Value;


			//ControlFlow.Assert(value != string.Empty, "Unable to locate a FrameWork value from the csproj file.  This is a required property. Project: " + project.Namecsproj);
			project.Frameworks = value.Split(";").ToList();
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
