using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Nuke.Common.Tooling;

namespace Nuke.Common.Tools.GitVersion
{
	public partial class GitVersionTasks
	{
		/// <summary>
		/// Returns the version of GitVersion installed if it found the app.  Returns EmptyString if it could not find or run GitVersion
		/// </summary>
		/// <param name="toolSettings"></param>
		/// <returns></returns>
		public static string Version(GitVersionSettings toolSettings = null) 
		{

			toolSettings = toolSettings ?? new GitVersionSettings();
			using var process = ProcessTasks.StartProcess(toolSettings);
			process.AssertWaitForExit();
			if ( process.ExitCode != 0 ) return "";

			// Last line of output should be the version
			if ( process.Output.Count == 0 ) {
				throw new ApplicationException("GitVersion:  Version - produced no output.  Expected 1 or more lines of output.");
			}

			Output value =  process.Output.Last();
			return value.Text;
		}

    }
}
