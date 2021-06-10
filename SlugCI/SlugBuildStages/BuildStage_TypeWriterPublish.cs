using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;


namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// Publishing stage for typewriter / npm output
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
		/// Run the typewriter publishing steps
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			string command = "npm";
			string npmArgs = "run publishTW";

			CompletionStatus = StageCompletionStatusEnum.InProcess;

			foreach ( SlugCIProject project in CISession.Projects ) {
				AddOutputText("Project: " + project.Name, OutputType.Std );
				AddOutputText("  --> HasTypeWriterScripts:  " + project.HasTypeWriterScripts,OutputType.Std );
				
				if ( !project.HasTypeWriterScripts )
					continue;
				

				AbsolutePath scriptsFolder = project.VSProject.Directory / "_scripts";
				IProcess process = ProcessTasks.StartProcess(command, npmArgs, scriptsFolder,customLogger: NPMLogger);
				process.AssertWaitForExit();

				StageOutput.AddRange(process.Output);

				if (process.ExitCode != 0) SetInprocessStageStatus(StageCompletionStatusEnum.Failure);
				else SetInprocessStageStatus(StageCompletionStatusEnum.Success);
			}

			if ( CompletionStatus == StageCompletionStatusEnum.InProcess ) CompletionStatus = StageCompletionStatusEnum.Success;

			return CompletionStatus;
		}



		/// <summary>
		/// NPM logs to informational messages to StdError.  Only know if NPM errored by checking return code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="output"></param>
		public static void NPMLogger(OutputType type, string output)
		{
			if (type == OutputType.Std)
				Logger.Normal(output);
			else if (output.StartsWith("npm notice"))
				Logger.Normal(output);
			else if (output.StartsWith("npm ERR:"))Logger.Error(output);
			else Logger.Normal(output);
		}
	}
}
