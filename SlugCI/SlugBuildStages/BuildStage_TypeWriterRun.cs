using System.Collections.Generic;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Cleaning stage
	/// </summary>
	class BuildStage_TypeWriterRun : BuildStage
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_TypeWriterRun (CISession ciSession) : base(BuildStageStatic.STAGE_TYPEWRITER, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_PUBLISH);
		}


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			string command = "npm";
			string npmArgs = "run pack_publish";

			CompletionStatus = StageCompletionStatusEnum.InProcess;

			foreach ( SlugCIProject project in CISession.Projects ) {
				StageOutput.Add(new Output { Text = "Project: " + project.Name, Type = OutputType.Std });
				StageOutput.Add(new Output { Text = "  --> HasTypeWriterScripts:  " + project.HasTypeWriterScripts });
				if ( !project.HasTypeWriterScripts )
					continue;
				

				AbsolutePath scriptsFolder = project.VSProject.Directory / "_scripts";
				IProcess process = ProcessTasks.StartProcess(command, npmArgs, scriptsFolder);
				process.AssertWaitForExit();

				StageOutput.AddRange(process.Output);

				if (process.ExitCode != 0) SetInprocessStageStatus(StageCompletionStatusEnum.Failure);
				else SetInprocessStageStatus(StageCompletionStatusEnum.Success);
			}

			if ( CompletionStatus == StageCompletionStatusEnum.InProcess ) CompletionStatus = StageCompletionStatusEnum.Success;

			return CompletionStatus;
		}
	}
}
