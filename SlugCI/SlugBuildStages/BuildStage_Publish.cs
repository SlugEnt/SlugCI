using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;
using Slug.CI.NukeClasses;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SlugEnt.CmdProcessor;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Compile stage
	/// </summary>
	public class BuildStage_Publish : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Publish(CISession ciSession) : base(BuildStageStatic.STAGE_PUBLISH, ciSession)
		{
			// TODO Need to figure out what to do here... It could be PUBLISH OR PUBLISH TEST or do we do a post processing Execution plan...?
			
			PredecessorList.Add(BuildStageStatic.STAGE_PACK);

			PredecessorList.Add(BuildStageStatic.STAGE_ANGULAR);
				
		}


		/// <summary>
		/// Run Compile process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess() {
			if ( CISession.SkipNuget && CISession.CountOfDeployTargetsNuget > 0) {
				Logger.Warn("Solution has Projects with a deploy method of Nuget Push, but the SkipNuget flag was set at command line.  Therefore skipping the deployment of Nuget packages");
			}
			if (!CISession.SkipNuget)
				Publish_Nuget();

			// Publish Visual Studio Projects
			Publish_Copy_VS();

			// Publish Angular Projects
			Publish_Copy_Angular();

			AOT_Success("Version: " + CISession.VersionInfo.SemVersionAsString + " fully committed and deployed to target location.");


			return CompletionStatus;
		}
		

		/// <summary>
		/// Copies from the compiled directory to the publish directory with the version as the last folder for Visual Studio projects..
		/// </summary>
		private void Publish_Copy_VS () {
			AOT_Normal("Publishing Visual Studio Projects");
			foreach (SlugCIProject project in CISession.Projects) {
				if ( project.Deploy != SlugCIDeployMethod.Copy ) continue;

				// Each framework needs to be copied.
				foreach ( string item in project.Frameworks ) {
					project.Results.PublishedSuccess = false;

					AbsolutePath destFolder = BuildDestinationFolderLayout_VS(project,item);

					AbsolutePath srcFolder = project.VSProject.Directory / "bin" / CISession.CompileConfig / item;
					FileSystemTasks.CopyDirectoryRecursively(srcFolder, destFolder, DirectoryExistsPolicy.Merge, FileExistsPolicy.OverwriteIfNewer);
					AOT_Success("Copied:  " + project.Name + " to Deployment folder: " + destFolder);
				}

				SetInprocessStageStatus(StageCompletionStatusEnum.Success);
				project.Results.PublishedSuccess = true;
			}
		}


		/// <summary>
		/// Publish Angular projects
		/// </summary>
		private void Publish_Copy_Angular() {
			AOT_Normal("Publishing Angular Projects");

			if (CISession.SkipAngularBuild)
				AOT_Warning("All Angular Projects were skipped due to manually set argument.");


			// Angular projects:  We only support single app publishes per project.  Angular NX supports more, we just don't have any examples at the moment.
			foreach (AngularProject project in CISession.SlugCIConfigObj.AngularProjects)
			{
				project.Results.PublishedSuccess = false;
				if ( CISession.SkipAngularBuild ) continue;

				// TODO - Replace "" with Angular Apps from dist folder 
				AbsolutePath destFolder = BuildDestinationFolderName_Angular(project, "");

				AbsolutePath angularDistPath = CISession.AngularDirectory / project.Name / "dist";
				AbsolutePath angularAppsSubFolder = angularDistPath / "apps";
				ControlFlow.Assert(Directory.Exists(angularDistPath),"Cannot find Angular dist folder.  Has Angular app been compiled?");

				AbsolutePath srcFolder = (AbsolutePath) "c:";

				// Determine where the source distribution files are.  This is either right in the dist folder OR there is a folder in dist called
				// "apps" with subfolders and the "web" is in each subfolder.
				if ( !Directory.Exists(angularAppsSubFolder) ) 
					srcFolder = angularDistPath;
				else {
					List<string> dirNames = Directory.GetDirectories(angularAppsSubFolder).ToList();
					ControlFlow.Assert(dirNames.Count > 0, "There are no compiled apps in the [" + angularAppsSubFolder + "].");
					if ( dirNames.Count == 1 ) srcFolder = (AbsolutePath) dirNames [0];
					else {
						ControlFlow.Assert(dirNames.Count < 2,
						                   "There is more than 1 app in the folder [" + angularAppsSubFolder + "].  We can only handle 1 at this time.");
					}
				}

				FileSystemTasks.CopyDirectoryRecursively(srcFolder, destFolder, DirectoryExistsPolicy.Merge, FileExistsPolicy.OverwriteIfNewer);
					AOT_Success("Copied:  " + project.Name + " to Deployment folder: " + destFolder);

				SetInprocessStageStatus(StageCompletionStatusEnum.Success);
				project.Results.PublishedSuccess = true;
			}
		}


		/// <summary>
		/// Computes the name of the Angular deployment folder for a given Angular Project app
		/// </summary>
		/// <param name="project"></param>
		/// <returns></returns>
		private AbsolutePath BuildDestinationFolderName_Angular (AngularProject project,string appName) {
			string rootName = project.Name;
/*			if ( CISession.SlugCIConfigObj.AngularDeployRootName == null || CISession.SlugCIConfigObj.AngularDeployRootName == string.Empty ) {
				// Name is the root directory name of project.
				rootName = Path.GetFileName(CISession.RootDirectory);
			}
			else
				rootName = CISession.SlugCIConfigObj.AngularDeployRootName;
*/
			if ( appName != string.Empty ) rootName = rootName + "." + appName;

			string versionFolder = "";

			if (CISession.SlugCIConfigObj.DeployToVersionedFolder)
			{
				if (CISession.SlugCIConfigObj.DeployFolderUsesSemVer) versionFolder = CISession.VersionInfo.SemVersionAsString;
				else versionFolder = CISession.VersionInfo.SemVersion.Major.ToString() + "." +
				                     CISession.VersionInfo.SemVersion.Minor.ToString() + "." +
				                     CISession.VersionInfo.SemVersion.Patch.ToString();
			}

			versionFolder = "Ver" + versionFolder;

			AbsolutePath destFolder = CISession.DeployCopyPath / rootName / versionFolder;
			return destFolder;
		}



		/// <summary>
		/// Builds the Destination folder path based upon the settings in the config file for Visual Studio Projects
		/// </summary>
		/// <param name="project"></param>
		/// <returns></returns>
		private AbsolutePath BuildDestinationFolderLayout_VS (SlugCIProject project, string framework) {
			string versionFolder = "";
			if ( CISession.SlugCIConfigObj.DeployToVersionedFolder ) {
				if ( CISession.SlugCIConfigObj.DeployFolderUsesSemVer ) versionFolder = CISession.VersionInfo.SemVersionAsString;
				else versionFolder = CISession.VersionInfo.SemVersion.Major.ToString() + "." +
					CISession.VersionInfo.SemVersion.Minor.ToString() + "." + 
					CISession.VersionInfo.SemVersion.Patch.ToString();
			}

			versionFolder = "Ver" + versionFolder;

			string projFolder = project.Name;

			// Now calculate main project folder name
			if ( CISession.SlugCIConfigObj.DeployToAssemblyFolders ) {
				string assemblyName = project.AssemblyName;
				projFolder = assemblyName.Replace('.', '\\');
			}
			
			// Build Path
			AbsolutePath destFolder = CISession.DeployCopyPath / projFolder / framework / versionFolder;
			return destFolder;
		}



		/// <summary>
		/// Publishes a Nuget package to a nuget site.
		/// </summary>
		private void Publish_Nuget () {
			DotNetNuGetPushSettings settings = new DotNetNuGetPushSettings()
			{
				Source = CISession.NugetRepoURL,
				ApiKey = CISession.NugetAPIKey,
				SkipDuplicate = true,
			};

			IReadOnlyCollection<AbsolutePath> nugetPackages = CISession.OutputDirectory.GlobFiles("*.nupkg");
			foreach (AbsolutePath nugetPackage in nugetPackages)
			{
				if (nugetPackage.ToString().EndsWith("symbols.nupkg")) continue;
				bool pushedSuccessfully = false;
				StageCompletionStatusEnum stepStatus = StageCompletionStatusEnum.NotStarted;

				try
				{
					settings.TargetPath = nugetPackage;
					IReadOnlyCollection<LineOutColored> nugetOutput = DotNetNuGetPush(settings);
					StageOutput.AddRange(nugetOutput);
					if (nugetOutput.Count > 0)
					{
						// Look for skipped message.
						foreach (ILineOut outputLine in nugetOutput)
						{
							if (outputLine.Text.Contains("already exists at feed")) {
								stepStatus = StageCompletionStatusEnum.Warning;
								string msg = @"A nuget package  <" +
								             Path.GetFileName(nugetPackage) +
								             ">  with this name and version already exists. " +
								             "Assuming this is due to you re-running the publish after a prior error that occurred after the push to Nuget was successful.  " +
								             "Will carry on as though this push was successful.  " +
								             "Otherwise, if this should have been a new update, then you will need to make another commit and re-publish";
								Logger.Warn(msg);
							}
							else if ( outputLine.Text.Contains("package was pushed") ) {
								pushedSuccessfully = true;
								stepStatus = StageCompletionStatusEnum.Success;
							}
						}
					}
				}
				catch (ProcessException pe)
				{
					stepStatus = StageCompletionStatusEnum.Failure;
					if ( !CISession.NugetRepoURL.Contains("nuget.org") ) 
						Logger.Warn(
							"The nuget Push process threw an error.  Since you are using a service other than Nuget this may be a service outage with the site or it might mean the version of the library you are pushing already exists.  You will need to perform a manual check to determine which it is.");
					else
						throw;
				}

				if ( pushedSuccessfully ) {
					string fileName = Path.GetFileName(nugetPackage);
					fileName = fileName.TrimEnd(".symbols.nupkg");
					fileName = fileName.TrimEnd(".nupkg");
					fileName = fileName.TrimEnd("." + CISession.VersionInfo.SemVersionAsString);
					fileName = fileName.ToLower();

					// Loop thru projects looking for that assembly name
					foreach ( SlugCIProject project in CISession.Projects ) {
						if ( project.AssemblyName.ToLower() == fileName || project.PackageId.ToLower() == fileName) {
							// For Tool Deployed projects, we need to copy the current version out to the deploy folder 
							if ( project.Deploy == SlugCIDeployMethod.Tool ) {
								AbsolutePath deployFile = CISession.DeployCopyPath / project.Name / CISession.PublishTarget.ToString() / "Version.json";
								ToolVersionJSON toolVersionJSON = new ToolVersionJSON() {ToolVersion = CISession.VersionInfo.SemVersionAsString};
								string json = JsonSerializer.Serialize<ToolVersionJSON>(toolVersionJSON, ToolVersionJSON.SerializerOptions());
								File.WriteAllText(deployFile, json);
							}
							project.Results.PublishedSuccess = true;
							break;
						}
					}
				}

				// Set stage status based upon Step Status
				SetInprocessStageStatus(stepStatus);
			}

			
		}
	}
}
