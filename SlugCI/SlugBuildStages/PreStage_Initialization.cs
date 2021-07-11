using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Slug.CI.NukeClasses;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// Perform some key startup tasks
	/// </summary>
	public class PreStage_Initialization : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public PreStage_Initialization (CISession ciSession) : base(BuildStageStatic.PRESTAGE_INITIALIZATION, ciSession) { }


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess () {
			// See if the Root directory exists
			ControlFlow.Assert(FileSystemTasks.DirectoryExists(CISession.RootDirectory),
			                   "Root Directory does not exist.  Should be specified on command line or be run from the projects entry folder");

			// See if slugci directory exists.
			if ( !CISession.IsInSetupMode )
				if ( !FileSystemTasks.DirectoryExists(CISession.SlugCIPath) || !FileSystemTasks.FileExists(CISession.SlugCIFileName) ) 
					{
					AOT_Error("Are you in the Repository root folder - where .git folder for the project is located at?");
					AOT_Error("Either change to the root folder of the project or run with --setup flag");
					return StageCompletionStatusEnum.Aborted;
				}
		


			// Load the SlugCI Config for the project
			CISession.SlugCIConfigObj = SlugCIConfig.LoadFromFile(CISession.SlugCIFileName);
			if ( !CISession.IsInSetupMode && CISession.SlugCIConfigObj == null) {
				CompletionStatus = StageCompletionStatusEnum.Failure;
				ControlFlow.Assert(CISession.SlugCIConfigObj != null, "Failure loading the SlugCI Configuration file - [ " + CISession.SlugCIFileName + " ]");
			}


			// See if version of config matches the version of SlugCI (our current instance)
			if (!CISession.IsInSetupMode)
				if ( CISession.SlugCI_Version != CISession.SlugCIConfigObj.ConfigStructureVersion ) {
					AOT_Warning("The version of the SlugCIConfig object does not match the current SlugCI version.  Setup will need to be run.");
					CISession.IsInSetupMode = true;
					return StageCompletionStatusEnum.Warning;
				}


			CheckForEnvironmentVariables();
			return StageCompletionStatusEnum.Success;
		}


		/// <summary>
		/// Checks to ensure environment variables are set.
		/// </summary>
		/// <returns></returns>
		private bool CheckForEnvironmentVariables()
		{
			List<string> requiredEnvironmentVariables = new List<string>()
			{
				SlugCI.ENV_SLUGCI_DEPLOY_PROD,
				SlugCI.ENV_SLUGCI_DEPLOY_BETA,
				SlugCI.ENV_SLUGCI_DEPLOY_ALPHA,
				SlugCI.ENV_SLUGCI_DEPLOY_DEV,
				SlugCI.ENV_NUGET_REPO_URL,
				SlugCI.ENV_NUGET_API_KEY
			};
				
			CISession.MissingEnvironmentVariables = new List<string>();

			foreach (string name in requiredEnvironmentVariables)
			{
				string result = Environment.GetEnvironmentVariable(name);
				if (result == null) CISession.MissingEnvironmentVariables.Add(name);
				else
				{
					CISession.EnvironmentVariables.Add(name, result);

					// Load the Environment Variables
					switch (name)
					{
						case SlugCI.ENV_NUGET_REPO_URL:
							CISession.NugetRepoURL = result;
							break;
						case SlugCI.ENV_NUGET_API_KEY:
							CISession.NugetAPIKey = result;
							break;
					}
				}
			}

			if (CISession.MissingEnvironmentVariables.Count == 0)
			{
				AOT_Success("All required environment variables found");
				return true;
			}

			AOT_NewLine();
			AOT_Warning("Some environment variables are missing.  These may or may not be required.");
			foreach (string item in CISession.MissingEnvironmentVariables)
				AOT_Warning("  -->  " + item);
			return false;
		}

	}
}
