using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CmdProcessor;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Restore Stage
	/// </summary>
	public class BuildStage_Restore : BuildStage
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ciSession"></param>
		public BuildStage_Restore (CISession ciSession) : base(BuildStageStatic.STAGE_RESTORE, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_CLEAN);
		}

		/// <summary>
		/// Run Restore Process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			DotNetRestoreSettings settings = new DotNetRestoreSettings();
			settings.ProjectFile = CISession.Solution;
			settings.Verbosity = DotNetVerbosity.Minimal;
			(BlockingCollection<ILineOut> outputs,int exitCode) = DotNetRestore(settings);
			StageOutput.AddRange(outputs);
			ControlFlow.Assert(exitCode == 0, "Process DotNetRestore failed");
			return StageCompletionStatusEnum.Success;
		}



		public  (BlockingCollection<ILineOut>, int) DotNetRestore(DotNetRestoreSettings toolSettings = null)
		{
			toolSettings = toolSettings ?? new DotNetRestoreSettings();
			ProcessStartInfo processStartInfo = SlugCmdProcess.GetDefaultProcessSettings();
			ToolSettingsToProcessInfoConverter.Convert(toolSettings, processStartInfo);
			SlugCmdProcess slugCmdProcess = new SlugCmdProcess("Dot Net Restore", processStartInfo);
			slugCmdProcess.Execute(DotNetRestore_OutputProcessor);

			return (slugCmdProcess.Output, slugCmdProcess.ExitCode);


			/*            using var process = ProcessTasks.StartProcess(toolSettings);
						process.AssertZeroExitCode();
						process.Output;
			*/
		}


		[CanBeNull]
		private LineOutColored? DotNetRestore_OutputProcessor(EnumProcessOutputType type, string text)
		{
			if (text == null) return null;
			EnumProcessOutputType processType = type;
			LineOutColored lineOutColored;
			
			if (type == EnumProcessOutputType.ProcessErr) return LineOutColored.Error(text);
			if (text.StartsWith("  Restored"))
				return LineOutColored.Success(text);
			if (text.Contains("up-to-date:")) return LineOutColored.Success(text);
			return LineOutColored.Normal(text);
		}

	}
}
