using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;


namespace Slug.CI.SlugBuildStages
	{
		/// <summary>
		/// Publishing stage for typewriter / npm output
		/// </summary>
		class BuildStage_Angular : BuildStage
		{
			/// <summary>
			/// Constructor
			/// </summary>
			public BuildStage_Angular(CISession ciSession) : base(BuildStageStatic.STAGE_ANGULAR, ciSession)
			{
				PredecessorList.Add(BuildStageStatic.STAGE_CALCVERSION);
			}


			/// <summary>
			/// Run the Angular Build
			/// </summary>
			/// <returns></returns>
			protected override StageCompletionStatusEnum ExecuteProcess()
			{
				string command = "ng";
				string ngArgs = "build";

				if ( CISession.SkipAngularBuild ) return StageCompletionStatusEnum.Skipped;

				CompletionStatus = StageCompletionStatusEnum.InProcess;

				foreach (AngularProject project in CISession.SlugCIConfigObj.AngularProjects)
				{
					AddOutputText("Project: " + project.Name, OutputType.Std);

					AbsolutePath angularProjectPath = CISession.AngularDirectory / project.Name;
					IProcess process = ProcessTasks.StartProcess(command, ngArgs, angularProjectPath, customLogger: AngularLogger);
					process.AssertWaitForExit();

					StageOutput.AddRange(process.Output);

					if ( process.ExitCode != 0 ) {
						SetInprocessStageStatus(StageCompletionStatusEnum.Failure);
						project.Results.CompileSuccess = false;
					}
					else {
						SetInprocessStageStatus(StageCompletionStatusEnum.Success);
						project.Results.CompileSuccess = true;
					}
				}

				if (CompletionStatus == StageCompletionStatusEnum.InProcess) CompletionStatus = StageCompletionStatusEnum.Success;

				return CompletionStatus;
			}



			/// <summary>
			/// NPM logs to informational messages to StdError.  Only know if NPM errored by checking return code.
			/// </summary>
			/// <param name="type"></param>
			/// <param name="output"></param>
			public static void AngularLogger(OutputType type, string output)
			{
				if (type == OutputType.Std)
					Logger.Normal(output);
				else if (output.StartsWith("npm notice"))
					Logger.Normal(output);
				else if (output.StartsWith("npm ERR:")) Logger.Error(output);
				else Logger.Normal(output);
			}
		}
	}

