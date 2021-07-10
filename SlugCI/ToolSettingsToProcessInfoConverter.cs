using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;

namespace Slug.CI
{
	public static class ToolSettingsToProcessInfoConverter
	{
		public static void Convert (ToolSettings toolSettings, ProcessStartInfo startInfo) {
			startInfo.FileName = toolSettings.ProcessToolPath;
			ControlFlow.Assert(startInfo.FileName != null, "ToolPath was not set.");

			// Push the tool arguments.
			startInfo.Arguments = toolSettings.GetProcessArguments().RenderForExecution();
			startInfo.WorkingDirectory = toolSettings.ProcessWorkingDirectory;
			
			ApplyEnvironmentVariables(toolSettings.ProcessEnvironmentVariables,startInfo);


/*
			if (!Path.IsPathRooted(toolPath) && !toolPath.Contains(Path.DirectorySeparatorChar))
				toolPath = ToolPathResolver.GetPathExecutable(toolPath);

			var toolPathOverride = GetToolPathOverride(toolPath);
			if (!string.IsNullOrEmpty(toolPathOverride))
			{
				arguments = $"{toolPath.DoubleQuoteIfNeeded()} {arguments}".TrimEnd();
				toolPath = toolPathOverride;
			}
*/
			
			ControlFlow.Assert(File.Exists(startInfo.FileName), $"ToolPath '{startInfo.FileName}' does not exist.");

        }

		private static void ApplyEnvironmentVariables([CanBeNull] IReadOnlyDictionary<string, string> environmentVariables, ProcessStartInfo startInfo)
		{
			if (environmentVariables == null)
				return;

			startInfo.Environment.Clear();

			foreach (var (key, value) in environmentVariables)
				startInfo.Environment[key] = value;
		}

	}
}
