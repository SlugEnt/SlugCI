using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

//using Nuke.Common.Tools.DotNet;
//using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
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
			if (!CISession.SkipNuget) {
				DotNetNuGetPushSettings settings = new DotNetNuGetPushSettings()
				{
					Source = CISession.NugetRepoURL,
					ApiKey = CISession.NugetAPIKey,
					SkipDuplicate = true,
				};

				IReadOnlyCollection<AbsolutePath> nugetPackages =  CISession.OutputDirectory.GlobFiles("*.nupkg");
				foreach ( AbsolutePath nugetPackage in nugetPackages ) {
					if (nugetPackage.ToString().EndsWith("symbols.nupkg")) continue;
					try {
						settings.TargetPath = nugetPackage;
						IReadOnlyCollection<Output> nugetOutput = DotNetNuGetPush(settings);
						if ( nugetOutput.Count > 0 ) {
							// Look for skipped message.
							foreach ( Output outputLine in nugetOutput ) {
								if ( outputLine.Text.Contains("already exists at feed") ) {
									string msg = @"A nuget package  <" +
									             Path.GetFileName(nugetPackage) +
									             ">  with this name and version already exists. " +
									             "Assuming this is due to you re-running the publish after a prior error that occurred after the push to Nuget was successful.  " +
									             "Will carry on as though this push was successful.  " +
									             "Otherwise, if this should have been a new update, then you will need to make another commit and re-publish";
									Logger.Warn(msg);
								}
								else { }

								CISession.PublishResults.Add(new PublishResultRecord("", "Nuget", nugetPackage));
							}
						}
					}
					catch ( ProcessException pe ) { 
						if ( !CISession.NugetRepoURL.Contains("nuget.org") ) 
							Logger.Warn(
								"The nuget Push process threw an error.  Since you are using a service other than Nuget this may be a service outage with the site or it might mean the version of the library you are pushing already exists.  You will need to perform a manual check to determine which it is.");
						else
							throw pe;
					}
				}
			}

			// TODO..
				// Now process Copy Outputs.
				//PublishCopiedFolders();

				//Logger.Success("Version: " + _gitProcessor.Version + " fully committed and deployed to target location.");
			

			return StageCompletionStatusEnum.Success;
		}
	}
}
