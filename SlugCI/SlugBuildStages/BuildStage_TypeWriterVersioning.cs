using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Semver;
using Slug.CI.NukeClasses;
using static Nuke.Common.IO.FileSystemTasks;


namespace Slug.CI.SlugBuildStages
{
	class BuildStage_TypeWriterVersioning : BuildStage {
		private const string CMD_PACK = @"del /f /q /s dist && tsc && npm run copy";
		private const string CMD_PUBLISH = @"cd dist && npm publish";
		private const string CMD_COPY = @"copy package.json dist";

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_TypeWriterVersioning (CISession ciSession) : base(BuildStageStatic.STAGE_TYPEWRITER_VER, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_TEST);
		}


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			// TODO REMOVE THIS - TEST ONLY
			//CISession.VersionInfo = new VersionInfo(new SemVersion(0,23,5,"alpha.1"),"g98g9k" );

			// Read the package.json file if necessary...
			foreach ( SlugCIProject project in CISession.Projects ) {
				StageOutput.Add(new Output
				{
					Text = "Project: " + project.Name,
					Type = OutputType.Std
				});
				AddOutputText("  --> HasTypeWriterScripts:  " + project.HasTypeWriterScripts, OutputType.Std);
				if ( !project.HasTypeWriterScripts ) continue;

				
				AbsolutePath scriptsFolder = project.VSProject.Directory / "_scripts";
				AddOutputText("  --> Scripts Folder: " + scriptsFolder, OutputType.Std );

				AbsolutePath scriptsFile = scriptsFolder / "package.json";
				TypeWriterConfig typeWriterConfig = null;
				if ( FileExists(scriptsFile) ) {
					string Json = File.ReadAllText(scriptsFile);
					typeWriterConfig = JsonSerializer.Deserialize<TypeWriterConfig>(Json, new JsonSerializerOptions{
						PropertyNameCaseInsensitive = true
					});
				}
				else {
					AddOutputText("  --> package.json file was not found.", OutputType.Err);
					ControlFlow.Assert(true == false,"Package.json file was not found");
				}


				// Make sure it has proper elements:
				bool updated = false;
				if ( typeWriterConfig.Scripts == null|| typeWriterConfig.Scripts.Pack != CMD_PACK ) {
					updated = true;
					typeWriterConfig.Scripts.Pack = CMD_PACK;
				}
				if (typeWriterConfig.Scripts.Publish != CMD_PUBLISH)
				{
					updated = true;
					typeWriterConfig.Scripts.Publish = CMD_PUBLISH;
				}
				if (typeWriterConfig.Scripts.Copy != CMD_COPY)
				{
					updated = true;
					typeWriterConfig.Scripts.Copy = CMD_COPY;
				}

				if ( typeWriterConfig.Version != CISession.VersionInfo.NPMVersion ) {
					updated = true;
					typeWriterConfig.Version = CISession.VersionInfo.NPMVersion;
					typeWriterConfig.VersionFull = CISession.VersionInfo.SemVersionAsString;
				}

				if ( updated ) {
					string json = JsonSerializer.Serialize<TypeWriterConfig>(typeWriterConfig, new JsonSerializerOptions {WriteIndented = true,PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
					File.WriteAllText(scriptsFile, json);
				}

				// Pack the files
				string command = "npm";
				string npmArgs = "run pack";
				IProcess process = ProcessTasks.StartProcess(command, npmArgs, scriptsFolder);
				process.AssertWaitForExit();
				StageOutput.AddRange(process.Output);

				if (process.ExitCode != 0) SetInprocessStageStatus(StageCompletionStatusEnum.Failure);
				else {
					CISession.GitProcessor.CommitChanges("TypeWriter Updates for project --> " + project.Name); 
					SetInprocessStageStatus(StageCompletionStatusEnum.Success);
				}


				SetInprocessStageStatus(StageCompletionStatusEnum.Success);
			}
			
			if (CompletionStatus == StageCompletionStatusEnum.InProcess) CompletionStatus = StageCompletionStatusEnum.Success;

			return CompletionStatus;
		}
	}
}
