using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;
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
		public static void WriteFinalHeader(StageCompletionStatusEnum status, CISession ciSession) {
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
			Console.WriteLine(hdrSep, lineColor);
			Console.WriteLine("|   " + "Project Step Status: ", lineColor);
			Console.WriteLine(hdrSep, lineColor);
			Console.WriteLine("");
			Console.WriteLine("  {0,-30}  {1,-8}  {2,-8}  {3,-8}  {4,-8}","Project","Deploy","Compile","Pack","Publish");
			foreach ( SlugCIProject project in ciSession.Projects ) {
				Console.WriteLine("  {0,-30}  {1,-8}  {2,-8}  {3,-8}  {4, -8}", 
				                  project.Name,
				                  project.Deploy.ToString(),
				                  WriteProjectStageStatus(project.Results.CompileSuccess),
				                  WriteProjectStageStatus(project.Results.PackedSuccess),
				                  WriteProjectStageStatus(project.Results.PublishedSuccess));
			}
			
		}


		public static string WriteProjectStageStatus (bool? value) {
			if ( value == null ) return "N/A";
			if ( value == true ) return "Success";
			return "Failed";

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
		

	}
}
