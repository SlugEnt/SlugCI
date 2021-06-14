using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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
				AOT_Normal("Project: " + project.Name, Color.Magenta );
				AOT_Normal("  --> HasTypeWriterScripts:  " + project.HasTypeWriterScripts,Color.Magenta );
				
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
		/// NPM logs to informational messages to StdError.  Only knows if NPM errored by checking return code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="output"></param>
		public void NPMLogger(OutputType type, string text)
		{
			if (type == OutputType.Std)
				AOT_Normal(text);
			else if (text.StartsWith("npm notice"))
				AOT_Info(text);
			else if (text.StartsWith("npm ERR:"))
				AOT_Error(text);
			AOT_Normal(text);
		}
	}
}
