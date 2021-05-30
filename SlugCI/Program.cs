using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;

namespace Slug.CI
{

	class Program
	{
		/// <summary>
		/// Main Entry Point
		/// <param name="rootdir">(Optional) The entry point folder for the solution - usually where the .git folder for the solution is.  If not specified will use current directory.</param>
		/// <param name="deployto">Where you are wanting to deploy this to.  Valid values are (dev, alpha, beta, prod)</param>
		/// <param name="compileconfig">This is the visual Studio Configuration value.  Standard values are Debug and Release.  But you can define your own also.
		/// <para>   If not specified then it will be set to Debug if the deployto is not production and set to Release if deployto is Production.</para></param>
		/// <param name="faststart">Skips checking the config file and updating it.  Generally should only be used when testing.</param>
		/// <param name="verbosity">Sets the verbosity of command output.  You can set value for all commands or just certain commands.  Be careful with all, it can generate a LOT of output on debug level
		/// <para>  The best is set to specific methods via:   method:value|method:value|method:value.</para>
		/// <para>  Valid methods are:</para>
		/// <para>    compile, pack, gitversion</para>
		/// <para>  Valid values are:</para>
		/// <para>    debug, warn, info</para></param>
		/// <param name="info">Displays detailed information about the config and the repo and other vital stats</param>
		/// </summary>
		/// <returns></returns>
		public static int Main(string rootdir = "", 
		                       string deployto = "test", 
		                       string compileconfig = "", 
		                       bool faststart = false, 
		                       string verbosity = "", 
		                       bool interactive = true,
		                       bool skipnuget = false,
		                       bool info = false) {
			try {
				Misc.WriteAppHeader();

				// We process thru defaults, at the end if interactive is on, then we
				// prompt user for changes / confirmation

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
					"alpha" => PublishTargetEnum.Alpha,
					"beta" => PublishTargetEnum.Beta,
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


				// Skip Nuget
				ciSession.SkipNuget = skipnuget;


				// Perform Validation 
				ValidateDependencies validation = new ValidateDependencies(ciSession);
				if (!validation.Validate()) throw new ApplicationException("One or more required features is missing from this pc.");


				// Interactive mode....
				if (interactive)
					UserPrompting(ciSession);


				// Create the SlugCI which is main processing class.
				SlugCI slugCI = new SlugCI(ciSession);

				if(interactive)
					Menu(ciSession, slugCI);


				// If user wanted info then display it and exit.
				if ( info ) {
					slugCI.DisplayInfo();
					return 0;
				}


				// Begin Executing
				slugCI.Execute();

				return 0;
			}
			catch ( Exception e ) {
				Logger.Error(e);
			}

			return 0;
		}


		/// <summary>
		/// Prompt user for information and confirm...
		/// </summary>
		private static bool UserPrompting (CISession ciSession) {
			Console.Clear();

			bool keepLooping = true;
			while ( keepLooping ) {
				PromptForDeployTarget(ciSession);
				PromptForConfiguration(ciSession);


				return true;
			}

			return true;
		}


		private static bool Menu (CISession ciSession, SlugCI slugCi) {
			bool keepLooping = true;
			while (keepLooping) {
				Console.WriteLine(Environment.NewLine);
				Color lineColor = Color.WhiteSmoke;
				ConsoleKey defaultChoice = ConsoleKey.Enter;

				Misc.WriteMainHeader("SlugCI Interactive Menu");

				Console.WriteLine(" (I)  Information about Project", Color.Yellow);
				Console.WriteLine();
				Console.WriteLine(" {0,-25}  |  {1,-30}", "Target Deploy", ciSession.PublishTarget.ToString());
				Console.WriteLine(" {0,-25}  |  {1,-30}", "Compile Configuration", ciSession.CompileConfig);
				Console.WriteLine();
				Console.WriteLine("Press Enter to start the Build Process");

				// Set Valid Keys
				List<ConsoleKey> validKeys = new List<ConsoleKey>()
				{
					ConsoleKey.I,
					ConsoleKey.Enter,
				};

				ConsoleKey answer = PromptAndGetResponse(ConsoleKey.Enter, validKeys);
				if ( answer == ConsoleKey.I ) slugCi.DisplayInfo();
				if ( answer == ConsoleKey.Enter ) return true;
			}

			return true;
		}


		/// <summary>
		/// Allow user to choose where they are deploying too.
		/// </summary>
		/// <param name="ciSession"></param>
		private static void PromptForConfiguration(CISession ciSession)
		{
			Console.WriteLine(Environment.NewLine);
			Color lineColor = Color.WhiteSmoke;
			ConsoleKey defaultChoice = ConsoleKey.Enter;

			List<string> help = new List<string>() { "Usually Release or Debug" };
			Misc.WriteMainHeader("Configuration to Compile", help);

			// Release
			lineColor = Color.WhiteSmoke;
			if (ciSession.CompileConfig == "Release")
			{
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.R;
			}
			else
				lineColor = Color.WhiteSmoke;
			Console.WriteLine("(R)  Release", lineColor);


			// Debug
			lineColor = Color.WhiteSmoke;
			if (ciSession.CompileConfig == "Debug")
			{
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.D;
			}
			else
				lineColor = Color.WhiteSmoke;
			Console.WriteLine("(D)  Debug", lineColor);


			// Other
			lineColor = Color.WhiteSmoke;
			if (ciSession.CompileConfig != "Debug" && ciSession.CompileConfig != "Release")
			{
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.O;
				Console.WriteLine("(O)  Other - " + ciSession.CompileConfig, lineColor);
			}



			// Set Valid Keys
			List<ConsoleKey> validKeys = new List<ConsoleKey>()
			{
				ConsoleKey.R,
				ConsoleKey.D,
				ConsoleKey.O
			};

			ConsoleKey choice = PromptAndGetResponse(defaultChoice, validKeys);
			if (choice == ConsoleKey.R) ciSession.CompileConfig = "Release";
			else if (choice == ConsoleKey.D)
				ciSession.CompileConfig = "Debug";
			else
				ciSession.CompileConfig = ciSession.CompileConfig;
		}



		/// <summary>
		/// Allow user to choose where they are deploying too.
		/// </summary>
		/// <param name="ciSession"></param>
		private static void PromptForDeployTarget (CISession ciSession) {
			Console.WriteLine(Environment.NewLine);
			Color lineColor = Color.WhiteSmoke;
			ConsoleKey defaultChoice = ConsoleKey.Enter;

			List<string> help = new List<string>() { "Where you are deploying the build" };
			Misc.WriteMainHeader("Deploy Target",help);
			
			// Production
			lineColor = Color.WhiteSmoke;
			if ( ciSession.PublishTarget == PublishTargetEnum.Production ) {
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.P;
			}
			else
				lineColor = Color.WhiteSmoke;
			Console.WriteLine("(P)  Production or main / master",lineColor);


			// Alpha
			lineColor = Color.WhiteSmoke;
			if (ciSession.PublishTarget == PublishTargetEnum.Alpha)
			{
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.A;
			}
			else
				lineColor = Color.WhiteSmoke;
			Console.WriteLine("(A)  Alpha or Test",lineColor);



			// Beta
			lineColor = Color.WhiteSmoke;
			if (ciSession.PublishTarget == PublishTargetEnum.Beta)
			{
				lineColor = Color.Green;
				defaultChoice = ConsoleKey.B;
			}
			else
				lineColor = Color.WhiteSmoke;
			Console.WriteLine("(B)  Beta / Test", lineColor);

			
			// Set Valid Keys
			List<ConsoleKey> validKeys = new List<ConsoleKey>()
			{
				ConsoleKey.A,
				ConsoleKey.B,
				ConsoleKey.P
			};

			ConsoleKey choice = PromptAndGetResponse(defaultChoice, validKeys);
			if ( choice == ConsoleKey.A ) ciSession.PublishTarget = PublishTargetEnum.Alpha;
			else if ( choice == ConsoleKey.B )
				ciSession.PublishTarget = PublishTargetEnum.Beta;
			else
				ciSession.PublishTarget = PublishTargetEnum.Production;
		}


		/// <summary>
		/// Displays the prompt and then accepts input from user.  Validates the input and returns the choice.
		/// </summary>
		/// <param name="defaultKey"></param>
		/// <param name="validKeys"></param>
		/// <returns></returns>
		private static ConsoleKey PromptAndGetResponse (ConsoleKey defaultKey,List<ConsoleKey> validKeys) {
			Console.WriteLine();
			Console.WriteLine("Press the letter of your choice, or Enter to accept current value.");
			bool keepLooping = true;
			while ( keepLooping ) {
				ConsoleKeyInfo choice = Console.ReadKey();
				if ( validKeys.Contains(choice.Key) ) return choice.Key;
				if ( choice.Key == ConsoleKey.Enter ) return defaultKey;
			}

			// This will never happen, but need to make compiler shut up!
			return defaultKey;
		}



		/// <summary>
		/// Sets verbosity of components based upon verbosity setting.
		/// </summary>
		/// <param name="verbosity"></param>
		/// <param name="ciSession"></param>
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
							ciSession.VerbosityGitVersion = ProcessVerbosity.All;
						else if ( splits [1] == "info" )
							ciSession.VerbosityGitVersion = ProcessVerbosity.Commands;
						else ciSession.VerbosityGitVersion = ProcessVerbosity.Nothing;
						break;
					case "calcversion":
						if ( splits [1] == "debug" )
							ciSession.VerbosityCalcVersion = Verbosity.Verbose;
						else
							ciSession.VerbosityCalcVersion = Verbosity.Normal;
						break;

				}
			}
		}
	}
}
