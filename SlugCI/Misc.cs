using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.Build.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;
using Console = Colorful.Console;


namespace Slug.CI
{
	public static class Misc
	{
		private const string hdrSep = "|-------------------------------------------------------------------|";
		private const string apphdr = "|###################################################################| ";


		/// <summary>
		/// Writes a sub section header.
		/// </summary>
		/// <param name="text"></param>
		public static void WriteSubHeader(string text, List<string> parameterList = null) {
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(hdrSep,Color.DodgerBlue);
			Console.WriteLine("|    " + text, Color.DarkCyan);
			if (parameterList != null)
				foreach (string param in parameterList)
				{
					Console.WriteLine("|   -->  " + param);
				}
			Console.WriteLine(hdrSep, Color.DodgerBlue);
			Console.WriteLine();
		}


		/// <summary>
		/// Writes A major section header
		/// </summary>
		/// <param name="text"></param>
		/// <param name="parameterList"></param>
		public static void WriteMainHeader(string text, List<string> parameterList = null)
		{
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(hdrSep, Color.DarkOrange);
			Console.WriteLine(hdrSep, Color.DarkOrange);
			Console.WriteLine("|    " + text, Color.DarkCyan);
			if (parameterList != null)
				foreach ( string param in parameterList ) {
					Console.WriteLine("|   -->  " + param,Color.Yellow);
				}
			Console.WriteLine(hdrSep, Color.DarkOrange);
			Console.WriteLine(hdrSep, Color.DarkOrange);
			Console.WriteLine();
		}




		/// <summary>
		/// Writes The Final Status information
		/// </summary>
		public static void WriteFinalHeader(StageCompletionStatusEnum status) {
			Color color;
			Color lineColor = Color.DarkViolet;

			if ( status == StageCompletionStatusEnum.Success ) color = Color.LimeGreen;
			else if ( status == StageCompletionStatusEnum.Failure || status == StageCompletionStatusEnum.Aborted )
				color = Color.Red;
			else
				color = Color.Yellow;

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();

			Console.WriteLine(apphdr, lineColor);
			Console.WriteLine(apphdr, lineColor);
			Console.WriteLine("|    " + "Overall Build Status: " + status, color);
			Console.WriteLine(apphdr, lineColor);
			Console.WriteLine(apphdr, lineColor);
			Console.WriteLine();
		}



		/// <summary>
		/// Writes the Application Header
		/// </summary>
		/// <param name="parameterList"></param>
		public static void WriteAppHeader(List<string> parameterList = null) {
			string hdrText = "SlugCI - Custom App Migrator";
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(apphdr, Color.DarkViolet);
			Console.WriteLine(hdrSep, Color.DarkViolet);
			Console.WriteLine("|    " + hdrText, Color.DarkCyan);
			if (parameterList != null)
				foreach (string param in parameterList)
				{
					Console.WriteLine("|   -->  " + param);
				}
			Console.WriteLine(hdrSep, Color.DarkViolet);
			Console.WriteLine(apphdr, Color.DarkViolet);
			Console.WriteLine();
		}



		const string ENV_GITVERSION = "GITVERSION_EXE";


		/// <summary>
		/// Ensures that GitVersion has a system wide environment variable set.  If not it will attempt to locate it and set the environment variable.
		/// </summary>
		/// <param name="targetEnvironment">Whether to target user or system/machine setting.  You must run the app as administrator to use Machine.</param>
		/// <returns></returns>
		// TODO This might not be needed anymore - Or is there a better way?
		public static bool ValidateGetVersionEnvVariable(EnvironmentVariableTarget targetEnvironment = EnvironmentVariableTarget.Process)
		{
			string envGitVersion = Environment.GetEnvironmentVariable(ENV_GITVERSION);


			if (envGitVersion == null)
			{
				Logger.Warn("GitVersion environment variable not found.  Will attempt to set.");

				string cmd = "where";
				string cmdArgs = "gitversion.exe";

				IProcess process = ProcessTasks.StartProcess(cmd, cmdArgs, logOutput: true);
				process.AssertWaitForExit();
				ControlFlow.Assert(process.ExitCode == 0, "The " + ENV_GITVERSION + " environment variable is not set and attempt to fix it, failed because it appears GitVersion is not installed on the local machine.  Install it and then re-run and/or set the environment variable manually");

				// Set the environment variable now that we found it
				string value = process.Output.First().Text;
				Environment.SetEnvironmentVariable(ENV_GITVERSION, value, targetEnvironment);
				envGitVersion = Environment.GetEnvironmentVariable(ENV_GITVERSION);
				string val = ToolPathResolver.TryGetEnvironmentExecutable("GITVERSION_EXE");
				Console.WriteLine("Toolpathresolver: " + val);
				Console.WriteLine();
				string msg =
					"GitVersion Environment variable has been set!  You will need to ensure you close the current console window before continuing to pickup the change.";
				Console.WriteWithGradient(msg, Color.Fuchsia, Color.Yellow, 16);
				Console.ReplaceAllColorsWithDefaults();
			}

			return true;
		}

	}
}
