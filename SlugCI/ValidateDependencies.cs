using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Drawing;
using System.Text;
using Newtonsoft.Json;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using Slug.CI.NukeClasses;
using YamlDotNet.Serialization.Schemas;
using Console = Colorful.Console;


namespace Slug.CI
{
	public class ValidateDependencies {
		private CISession CISession { get; set; }


		public ValidateDependencies (CISession ciSession) {
			CISession = ciSession;
		}


		public bool Validate () {
			Misc.WriteMainHeader("Validate:", new List<string>() {"Confirm dependencies are installed and working"});

			bool success = true;
			success = Validate_GitVersion();
			return success;
		}


		public bool Validate_GitVersion () {
			GitVersionSettings settings = new GitVersionSettings()
			{
				ProcessWorkingDirectory = CISession.RootDirectory,
				Framework = "netcoreapp3.1",
				NoFetch = false,
				ProcessLogOutput = false,
				Verbosity = GitVersionVerbosity.info,
				Version = true,
			};

			string version = GitVersionTasks.Version(settings);
			if ( version != string.Empty ) {
				Console.WriteLine("  [Ok]    -->  GitVersion:  " + version, Color.DarkGreen);
				return true;
			}

			Console.WriteLine( "  [Fail]  -->  GitVersion:  Appears to not be installed or is not running correctly", Color.Red);
			return false;
		}
	}
}
