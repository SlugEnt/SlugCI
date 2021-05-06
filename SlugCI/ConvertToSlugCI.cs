using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Slug.CI;
using SlugCI;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Console = Colorful.Console;

namespace Slug.CI
{
	/// <summary>
	/// Takes an existing solution setup that is not SlugCI and converts it to the SlugCI structure.
	/// Also validates the existing SlugCI and performs any updated to config values.
	/// </summary>
	public class ConvertToSlugCI {
		public const string SLUG_CI_CONFIG_FILE = "SlugCI_Config.json";

		/// <summary>
		/// The solution projects Main folder.
		/// </summary>
		public AbsolutePath RootDirectory { get; set; }

		/// <summary>
		/// The RootCI folder
		/// </summary>
		public AbsolutePath SlugCIPath { get; set; }

		/// <summary>
		/// Full path and name to the SlugCI config file
		/// </summary>
		public AbsolutePath SlugCIFileName { get; set; }


		public AbsolutePath SourceDirectory { get; set; }

		public AbsolutePath TestsDirectory { get; set; }
		public AbsolutePath OutputDirectory { get; set; }

		internal AbsolutePath CurrentSolutionPath { get; set; }
		internal AbsolutePath ExpectedSolutionPath { get; set; }
		

		internal string DotNetPath { get; set; }


		private readonly List<VisualStudioProject> Projects = new List<VisualStudioProject>();

		/// <summary>
		/// True if the solution folder structure is setup for .Nuke / SlugNuke
		/// </summary>
		private bool IsSlugNukeFormat { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public ConvertToSlugCI(AbsolutePath rootDir)
		{
			// Set expected directories.
			RootDirectory = rootDir;
			SlugCIPath = RootDirectory / ".slugci";
			SlugCIFileName = SlugCIPath / SLUG_CI_CONFIG_FILE;
			SourceDirectory = RootDirectory / "src";
			TestsDirectory = RootDirectory / "tests";
			OutputDirectory = RootDirectory / "artifacts";
			ExpectedSolutionPath = SourceDirectory;

			Misc.WriteHeader("PreCheck");
			
			PreCheck();
		}


		public bool PreCheck()
		{
			// See if the Root directory exists
			ControlFlow.Assert(FileSystemTasks.DirectoryExists(RootDirectory), "Root Directory does not exist.  Should be specified on command line or be run from the projects entry folder");

			if ( DirectoryExists(RootDirectory / ".nuke") || FileExists(RootDirectory / ".nuke")) {
				IsSlugNukeFormat = true;
				Logger.Warn("Detected previous SlugNuke Solution.  Will convert to SlugCI!");
			}

		
			if (!FileSystemTasks.DirectoryExists(SlugCIPath))
			{
				Logger.Warn(".SlugCI directory does not exist.  Proceeding with converting solution to .SlugCI format specifications");
				// Need to convert project to SlugCI layout.

			}

			
			ControlFlow.Assert(Converter(),"Failure during SlugCI Converted processing.");
			return true;
		}



		/// <summary>
		/// Performs initial logic to ensure that the Solution is ready for the SlugCI build process.
		///   - Solution is in the proper directory structure
		///     - If not, it will move it to the proper structure
		///   - Ensure a .SlugCI file exists.
		/// </summary>
		/// <returns></returns>
		public bool Converter()
		{
			// Find the Solution - Assume we are in the root folder right now.
			List<string> solutionFiles = SearchForSolutionFile(RootDirectory.ToString(), ".sln");
			ControlFlow.Assert(solutionFiles.Count != 0, "Unable to find the solution file");
			ControlFlow.Assert(solutionFiles.Count == 1, "Found more than 1 solution file under the root directory -  - We can only work with 1 solution file." + RootDirectory.ToString());
			string solutionFile = solutionFiles[0];
			Logger.Normal("Solution File found:  {0}", solutionFile);

			// A.  Proper Directory Structure
			ControlFlow.Assert(ProperDirectoryStructure(solutionFile), "Attempted to put solution in proper directory structure, but failed.");

			// B.  Nuke File Exists
			// TODO Remove - not needed anymore
			//ControlFlow.Assert(NukeFileIsProper(solutionFile), "Unable to format Nuke file in proper format");

			// C.  Ensure the NukeSolutionBuild file is setup.
			if ( IsSlugNukeFormat ) {
				// Move the config file to new name and location.  Delete no longer needed files.
				try {
					AbsolutePath oldNukeFile = RootDirectory / "nukeSolutionBuild.conf";
					EnsureExistingDirectory(SlugCIPath);
					if (!FileExists(SlugCIFileName)) 
						MoveFile(oldNukeFile, SlugCIFileName);
					else if ( FileExists(oldNukeFile) ) {
						Logger.Warn(SlugCIFileName +
						            " already exists.  But so too does the old Config file.  Assuming this was from a prior error.  Removing the old file and LEAVING the current file intact.  Please check to ensure it is correct");
						DeleteFile(oldNukeFile);
					}

					DeleteFile(RootDirectory / ".nuke");
					DeleteFile(RootDirectory / "build.cmd");
					DeleteFile(RootDirectory / "build.ps1");
					DeleteFile(RootDirectory / "build.sh");
					DeleteFile(RootDirectory / "global.json");
					IsSlugNukeFormat = false;
				}
				catch ( Exception e ) {
					Logger.Warn("Failed to move the old SlugNuke Config file to new SlugCI file.");
					Logger.Error(e);
				}
			}
			
			
			// Ensure Config file is valid and up-to-date with current Class Structure
			ProcessSlugCIConfigFile();

			// D.  Copy the GitVersion.Yml file
			// TODO Remove this code - not necessary?
			/*
			Assembly assembly = Assembly.GetExecutingAssembly();
			string assemblyFile = assembly.Location;
			string assemblyFolder = Path.GetDirectoryName(assemblyFile);
			Logger.Info("Assembly Folder: " + assemblyFolder);
			string src = Path.Combine(assemblyFolder, "GitVersion.yml");
			AbsolutePath dest = RootDirectory / "GitVersion.yml";

			if (!FileExists(dest))
				File.Copy(src, dest, false);


			// E.  Ensure GitVersion.exe environment variable exists
			*/
			return true;
		}



		/// <summary>
		/// Ensures there is a valid SlugCI Config file and updates it if necessary OR creates it.
		/// </summary>
		/// <returns></returns>
		private bool ProcessSlugCIConfigFile() {
			bool isNewConfigFile = false;
			string startingJson;

			SlugCIConfig slugCiConfig;
			if (FileExists(SlugCIFileName)) {
				startingJson = File.ReadAllText(SlugCIFileName);
				slugCiConfig = JsonSerializer.Deserialize<SlugCIConfig>(startingJson, SlugCIConfig.SerializerOptions());
				//using (FileStream fs = File.OpenRead(nsbFile)) { slugCiConfig = await JsonSerializer.DeserializeAsync<SlugCIConfig>(fs, SlugCIConfig.SerializerOptions()); }
			}
			else
			{
				EnsureExistingDirectory(SlugCIPath);
				slugCiConfig = new SlugCIConfig();
				slugCiConfig.DeployToVersionedFolder = true;
				isNewConfigFile = true;
			}

			// Make a copy that we will compare against.
			SlugCIConfig origSlugCiConfig = slugCiConfig.Copy();


			bool updateProjectAdd = false;
			bool hasCopyDeployMethod = false;

			// Now go thru the projects and update the config
			foreach (VisualStudioProject project in Projects)
			{
				Slug.CI.Project slugCIProject = slugCiConfig.GetProjectByName(project.Name);
				if (slugCIProject == null)
				{
					updateProjectAdd = true;
					slugCIProject = new Project() { Name = project.Name };
					slugCIProject.Framework = project.Framework;
					slugCIProject.IsTestProject = project.IsTestProject;

					if (project.IsTestProject)
					{
						slugCIProject.Deploy = SlugCIDeployMethod.None;

						// Also add the Required Nuget Coverage package
						CoverletInstall(project);
					}
					else
						slugCIProject.Deploy = SlugCIDeployMethod.Copy;

					slugCiConfig.Projects.Add(slugCIProject);
				}
				else
				{
					// Check for updated values:
					if (slugCIProject.Framework != project.Framework)
					{
						slugCIProject.Framework = project.Framework;
					}

					if (slugCIProject.IsTestProject)
						// Also add the Required Nuget Coverage package
						CoverletInstall(project);

					if (slugCIProject.Deploy == SlugCIDeployMethod.Copy) hasCopyDeployMethod = true;
				}
			}


			// Ensure Deploy Roots have values if at least one of the projects has a deploy method of Copy
			if (hasCopyDeployMethod) {
				bool wroteHelpText = false;
				bool checkedForEnvironmentVariables = false;
				bool environmentVariablesExist = false;

				for (int i = 0; i < 2; i++)
				{
					string name;
					Configuration config;

					if (i == 0)
					{
						name = "Production";
						config = Configuration.Release;
					}
					else
					{
						name = "Test";
						config = Configuration.Debug;
					}

					if (!slugCiConfig.IsRootFolderSpecified(config))
					{
						if ( !wroteHelpText ) {
							// See if Environment Variables are set.  We only check once.
							if ( !checkedForEnvironmentVariables ) {
								string value = EnvironmentInfo.GetParameter<string>("SLUGCIDEPLOYPROD");
								string value2 = EnvironmentInfo.GetParameter<string>("SLUGCIDEPLOYTEST");
								if ( !value.IsNullOrEmpty() && !value2.IsNullOrEmpty()) environmentVariablesExist = true;
								checkedForEnvironmentVariables = true;
							}


							Console.WriteLine();
							Console.WriteLine("One or both of the Deployment folder entries is missing.  This is is ok, if there are environment variables set for these values. Environment Variable values WILL override the values in the config.",Color.DarkOrange);
							Console.WriteLine("--: If you do not wish to enter values for these and instead rely on Environment variables, just hit enter.", Color.DarkOrange);
							Console.WriteLine("I did NOT detect one or both of the environment variables.  You need to set these manually",Color.Yellow);
							if (!environmentVariablesExist)
								Console.WriteLine("The environment variables are:  SLUGCI_DEPLOY_PROD and SLUGCI_DEPLOY_TEST", Color.DeepPink);
							wroteHelpText = true;
						}
						Console.WriteLine();
						Console.WriteLine("Enter the root deployment folder for {0} [{1}]", Color.DarkCyan, name, config);
						string answer = Console.ReadLine();
						if (i == 0)
							slugCiConfig.DeployProdRoot = answer;
						else
							slugCiConfig.DeployTestRoot = answer;
					}
				}
			}


			// We now always write the config file at the end of Setup.  This ensure we get any new properties.
			if ( origSlugCiConfig != slugCiConfig ) {
				string json = JsonSerializer.Serialize<SlugCIConfig>(slugCiConfig, SlugCIConfig.SerializerOptions());
				File.WriteAllText(SlugCIFileName, json);
				if ( updateProjectAdd ) {
					Logger.Warn("The file: {0} was updated.  One ore more projects were added.  Ensure they have the correct Deploy setting.", SlugCIFileName);
				}
			}

			return true;
		}



		/// <summary>
		/// Ensures the Nuke file first line has the solution in the right format.
		/// </summary>
		/// <param name="solutionFile"></param>
		/// <returns></returns>
		private bool NukeFileIsProper(string solutionFile)
		{
			string expectedNukeLine = Path.GetFileName(ExpectedSolutionPath) + "/" + Path.GetFileName(solutionFile);

			// Read Nuke File if it exists.
			AbsolutePath nukeFile = RootDirectory / ".nuke";
			if (FileExists(nukeFile))
			{
				string[] lines = File.ReadAllLines(nukeFile.ToString(), Encoding.ASCII);
				if (lines.Length != 0)
					if (lines[0] == expectedNukeLine)
						return true;
			}

			// If here the file does not exist or in wrong format.
			File.WriteAllText(nukeFile, expectedNukeLine);
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
			if (!DirectoryExists(SourceDirectory)) { Directory.CreateDirectory(SourceDirectory.ToString()); }

			// Create Tests folder if it does not exist.
			if (!DirectoryExists(TestsDirectory)) { Directory.CreateDirectory(TestsDirectory.ToString()); }

			// Create Artifacts / Output folder if it does not exist.
			if (!DirectoryExists(OutputDirectory)) { Directory.CreateDirectory(OutputDirectory.ToString()); }

			// Query the solution for the projects that are in it.
			// We allow all tests to run, instead of failing at first failure.
			CurrentSolutionPath = (AbsolutePath)Path.GetDirectoryName(solutionFile);

			DotNetPath = ToolPathResolver.GetPathExecutable("dotnet");
			IProcess slnfind = ProcessTasks.StartProcess(DotNetPath, "sln " + CurrentSolutionPath + " list", logOutput: true);
			slnfind.AssertWaitForExit();
			IReadOnlyCollection<Output> output = slnfind.Output;


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
			if (CurrentSolutionPath.ToString() != ExpectedSolutionPath.ToString()) solutionNeedsToMove = true;

			List<VisualStudioProject> movedProjects = new List<VisualStudioProject>();
			// Step 3
			foreach (Output outputRec in output)
			{
				if (outputRec.Text.EndsWith(".csproj"))
				{
					VisualStudioProject project = GetInitProject(outputRec.Text);
					Projects.Add(project);

					// Do we need to move the project?
					if ((project.OriginalPath.ToString() != project.NewPath.ToString()) || solutionNeedsToMove)
					{
						movedProjects.Add(project);
						MoveProjectStepA(project);
					}
				}
			}

			// Step 4:  Is Solution in proper directory.  If not move it.
			if (solutionNeedsToMove)
			{
				string slnFileCurrent = CurrentSolutionPath / Path.GetFileName(solutionFile);
				string slnFileFuture = ExpectedSolutionPath / Path.GetFileName(solutionFile);
				File.Move(slnFileCurrent, slnFileFuture);
			}


			// Step 5.  Read project to solution
			if (movedProjects.Count > 0)
			{
				foreach (VisualStudioProject project in movedProjects) { MoveProjectStepB(project); }
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
			IProcess sln = ProcessTasks.StartProcess(DotNetPath, "sln " + CurrentSolutionPath + " remove " + removeParam, logOutput: true);
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

			Logger.Success("Project: {0} successfully relocated into proper new directory layout.", project.Name);
			return true;
		}


		/// <summary>
		/// Takes the current Project path and creates an official VisualStudioProject object from it.
		/// </summary>
		/// <param name="path">Path as returned from "dotnet sln" command</param>
		/// <returns></returns>
		public VisualStudioProject GetInitProject(string path)
		{
			VisualStudioProject visualStudioProject = new VisualStudioProject()
			{
				Namecsproj = Path.GetFileName(path),
				Name = Path.GetFileName(Path.GetDirectoryName(path))
			};


			string lcprojName = visualStudioProject.Name.ToLower();

			AbsolutePath newRootPath = ExpectedSolutionPath;
			if (lcprojName.StartsWith("test") || lcprojName.EndsWith("test"))
			{
				visualStudioProject.IsTestProject = true;
				newRootPath = TestsDirectory;
			}

			visualStudioProject.OriginalPath = (AbsolutePath)Path.GetDirectoryName(Path.Combine(CurrentSolutionPath, path));
			visualStudioProject.NewPath = newRootPath / visualStudioProject.Name;


			// Determine Framework type.
			DetermineFramework(visualStudioProject);
			return visualStudioProject;
		}



		/// <summary>
		/// Determines the Project's targeted framework.
		/// </summary>
		/// <param name="project"></param>
		private void DetermineFramework(VisualStudioProject project)
		{
			// Determine csproj path
			AbsolutePath csprojPath = project.OriginalPath / project.Namecsproj;


			XDocument doc = XDocument.Load(csprojPath);
			string value = doc.XPathSelectElement("//PropertyGroup/TargetFramework").Value;
			ControlFlow.Assert(value != string.Empty, "Unable to locate a FrameWork value from the csproj file.  This is a required property. Project: " + project.Namecsproj);
			project.Framework = value;
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
			DotNet("add package coverlet.msbuild", csprojPath);
			return true;
		}



		/// <summary>
		/// Looks for the .sln file in the current folder and all subdirectories.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		List<string> SearchForSolutionFile(string root, string searchTerm)
		{
			var files = new List<string>();

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
