using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

//using Nuke.Common.Tools.DotNet;
//using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;
using static Nuke.Common.IO.FileSystemTasks;
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

			Publish_Copy();
			Logger.Success("Version: " + CISession.VersionInfo.SemVersionAsString + " fully committed and deployed to target location.");
			

			return StageCompletionStatusEnum.Success;
		}
		

		/// <summary>
		/// Copies from the compiled directory to the publish directory with the version as the last folder..
		/// </summary>
		private void Publish_Copy () {
			foreach (SlugCIProject project in CISession.Projects) {
				if ( project.Deploy != SlugCIDeployMethod.Copy ) continue;

				project.Results.PublishedSuccess = false;

				// Convert the Assembly name into folders...
				string assemblyName = project.AssemblyName;
				string assemblyAsFolders = assemblyName.Replace('.', '\\');
				AbsolutePath destFolder = CISession.DeployCopyPath / assemblyAsFolders / CISession.VersionInfo.SemVersionAsString;

				AbsolutePath srcFolder = project.VSProject.Directory / "bin" / CISession.CompileConfig / project.Framework;
				FileSystemTasks.CopyDirectoryRecursively(srcFolder,destFolder,DirectoryExistsPolicy.Merge,FileExistsPolicy.OverwriteIfNewer);
				Logger.Success("Copied:  " + assemblyName + " to Deployment folder: " + destFolder);
				project.Results.PublishedSuccess = true;
			}
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
					IReadOnlyCollection<Output> nugetOutput = DotNetNuGetPush(settings);
					if (nugetOutput.Count > 0)
					{
						// Look for skipped message.
						foreach (Output outputLine in nugetOutput)
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
						throw pe;
				}

				if ( pushedSuccessfully ) {
					string fileName = Path.GetFileName(nugetPackage);
					fileName = fileName.TrimEnd(".symbols.nupkg");
					fileName = fileName.TrimEnd(".nupkg");
					fileName = fileName.TrimEnd("." + CISession.VersionInfo.SemVersionAsString);
					// Loop thru projects looking for that assembly name
					foreach ( SlugCIProject project in CISession.Projects ) {
						if ( project.AssemblyName == fileName ) {
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
