using System;
using System.Collections.Generic;
using System.Text;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;

namespace Slug.CI.SlugBuildStages
{
	/// <summary>
	/// The DotNet Compile stage
	/// </summary>
	public class BuildStage_Compile : BuildStage {

		/// <summary>
		/// Constructor
		/// </summary>
		public BuildStage_Compile (CISession ciSession) : base(BuildStageStatic.STAGE_COMPILE, ciSession) {
			PredecessorList.Add(BuildStageStatic.STAGE_CALCVERSION);
		}


		/// <summary>
		/// Run Compile process
		/// </summary>
		/// <returns></returns>
		protected override StageCompletionStatusEnum ExecuteProcess()
		{
			DotNetBuildSettings dotNetBuildSettings = new DotNetBuildSettings()
			{
				ProjectFile = CISession.Solution,
				NoRestore = true,
				PropertiesInternal = new Dictionary<string, object>(),
			};

			dotNetBuildSettings = dotNetBuildSettings.SetProjectFile(CISession.Solution)
			                                         .SetConfiguration(CISession.CompileConfig)
			                                         .SetVerbosity(DotNetVerbosity.Minimal)
			                                         .EnableNoRestore()
			                                         .SetAssemblyVersion(CISession.VersionInfo.AssemblyVersion)
			                                         .SetFileVersion(CISession.VersionInfo.FileVersion);

			IReadOnlyCollection<Output> compileOut = DotNetTasks.DotNetBuild(dotNetBuildSettings);
			StageOutput.AddRange(compileOut);

			Console.WriteLine();
			Console.WriteLine("Compilation Success:");
			foreach ( SlugCIProject project in CISession.SlugCIConfigObj.Projects ) {
				foreach ( Output output in compileOut ) {
					if ( output.Text.StartsWith("  " + project.Name + " -> ") ) {
						Logger.Success("Compile Success:  " + output.Text);
						project.Results.CompileSuccess = true;
						break;
					}
				}
			}
			return StageCompletionStatusEnum.Success;
		}
	}
}
