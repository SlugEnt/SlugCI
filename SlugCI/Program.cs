using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Slug.CI.NukeClasses;

namespace Slug.CI
{
	
	class Program : NukeBuild
	{
		/// <summary>
		/// Main Entry Point
		/// <param name="rootdir">(Optional) The entry point folder for the solution - usually where the .git folder for the solution is.  If not specified will use current directory.</param>
		/// <param name="deployto">Where you are wanting to deploy this to.  Valid values are (dev, test, prod)</param>
		/// <param name="compileconfig">This is the visual Studio Configuration value.  Standard values are Debug and Release.  But you can define your own also.
		/// <para>   If not specified then it will be set to Debug if the deployto is not production and set to Release if deployto is Production.</para></param>
		/// <param name="faststart">Skips checking the config file and updating it.  Generally should only be used when testing.</param>
		/// <param name="verbosity">Sets the verbosity of command output.  You can set value for all commands or just certain commands.  Be careful with all, it can generate a LOT of output on debug level
		/// <para>  The best is set to specific methods via:   method:value|method:value|method:value.</para>
		/// <para>  Valid methods are:</para>
		/// <para>    compile, pack, gitversion</para>
		/// <para>  Valid values are:</para>
		/// <para>    debug, warn, info</para></param>
		/// 
		/// </summary>
		/// <returns></returns>
		public static int Main(string rootdir = "", string deployto = "test", string compileconfig = "", bool faststart = false, string verbosity = "", bool interactive = true) {
			try {
				Misc.WriteAppHeader();
				CISession ciSession = new CISession();

				// If no RootDir specified, then set to current directory.
				if ( rootdir == string.Empty )
					ciSession.RootDirectory = (AbsolutePath) Directory.GetCurrentDirectory();
				else
					ciSession.RootDirectory = (AbsolutePath) rootdir;



				// Determine Deployment Target
				deployto = deployto.ToLower();
				ciSession.PublishTarget = deployto switch
				{
					"test" => PublishTargetEnum.Testing,
					"dev" => PublishTargetEnum.Development,
					"prod" => PublishTargetEnum.Production,
					_ => PublishTargetEnum.Development,
				};



				// Set Compile Configuration.  If not specified, then we base it upon PublishTarget.  This ensure production does not have Debug code, unless specifically requested.
				if ( compileconfig == string.Empty ) {
					if ( ciSession.PublishTarget == PublishTargetEnum.Production )
						ciSession.CompileConfig = "Release";
					else
						ciSession.CompileConfig = "Debug";
				}
				else
					ciSession.CompileConfig = compileconfig;


				// Set the Verbosity of components
				SetVerbosity(verbosity,ciSession);


				// Set Faststart
				if ( faststart == true ) ciSession.IsFastStart = true;


				// Interactive mode
				ciSession.IsInteractiveRun = interactive;


				// Create the SlugCI which is main processing class.
				SlugCI slugCI = new SlugCI(ciSession);


				// Perform Validation 
				ValidateDependencies validation = new ValidateDependencies(ciSession);
				if (!validation.Validate()) throw new ApplicationException("One or more required features is missing from this pc.");


				// Begin Executing
				slugCI.Execute();

				return 0;


				Console.WriteLine("Hello World!");
			}
			catch ( Exception e ) {
				Logger.Error(e);
			}

			return 0;
		}


		private static void SetVerbosity (string verbosity, CISession ciSession) {
			List<string> methods = verbosity.Split('|').ToList();
			foreach ( string method in methods ) {
				string [] splits = method.Split(':');
				if ( splits.Length != 2 ) throw new ArgumentException("Verbosity setting has invalid method:value combination of:  " + method);

				// Validate the possible values
				if (splits[1] != "debug" && splits[1] != "info" && splits[1] != "warn") throw new ArgumentException("Verbosity setting has invalid value for the method:  " + method);

				switch ( splits [0] ) {
					case "compile":
						if (splits[1] == "debug") 
							ciSession.VerbosityCompile = DotNetVerbosity.Diagnostic;
						else if ( splits [1] == "info" ) ciSession.VerbosityCompile = DotNetVerbosity.Normal;
						else ciSession.VerbosityCompile = DotNetVerbosity.Detailed;
						break;
					case "pack":
						if (splits[1] == "debug")
							ciSession.VerbosityPack = DotNetVerbosity.Diagnostic;
						else if (splits[1] == "info") ciSession.VerbosityPack = DotNetVerbosity.Normal;
						else ciSession.VerbosityPack = DotNetVerbosity.Detailed;
						break;
					case "gitversion":
						if (splits[1] == "debug")
							ciSession.VerbosityGitVersion = GitVersionVerbosity.debug;
						else if ( splits [1] == "info" )
							ciSession.VerbosityGitVersion = GitVersionVerbosity.info;
						else ciSession.VerbosityGitVersion = GitVersionVerbosity.warn;
						break;

				}
			}
		}
	}
}
