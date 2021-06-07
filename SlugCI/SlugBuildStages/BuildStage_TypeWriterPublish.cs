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
	class BuildStage_TypeWriterPublish : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_TypeWriterPublish (CISession ciSession) : base(BuildStageStatic.STAGE_TYPEWRITER_PUBLISH, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_PUBLISH);
		}


		/// <summary>
		/// Run Clean process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			string command = "npm";
			string npmArgs = "run publish";

			CompletionStatus = StageCompletionStatusEnum.InProcess;

			foreach ( SlugCIProject project in CISession.Projects ) {
				AddOutputText("Project: " + project.Name, OutputType.Std );
				AddOutputText("  --> HasTypeWriterScripts:  " + project.HasTypeWriterScripts,OutputType.Std );
				
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
