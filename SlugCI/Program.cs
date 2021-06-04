using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;

namespace Slug.CI
{
	class Program
	{
		/// <summary>
		/// SlugCI - An extremely opinionated Git Versioning and CI tool.
		/// </summary>
		/// <param name="rootdir">(Optional) The folder that contains the GIT Repository for the solution.</param>
		/// <param name="deployto">Where you are wanting to deploy this to.  Valid values are (dev, alpha, beta, prod)</param>
		/// <param name="compileconfig">The visual Studio Configuration value.  Standard values are Debug and Release.  But you can define your own also.
		/// <para>   If not specified then it will be set to Debug if the deployto is not production and set to Release if deployto is Production.</para></param>
		/// <param name="faststart">Skips checking the config file and updating it.  Generally should only be used when testing.</param>
		/// <param name="interactive">If True (Default) will prompt you for values.</param>
		/// <param name="skipnuget">Does not publish to nuget.  Still builds the .nupkg however.</param>
		/// <param name="info">Displays detailed information about the Solution and Environment</param>
		/// <param name="verbosity">Sets the verbosity of command output.  You can set value for all commands or just certain commands.  Be careful with all, it can generate a LOT of output on debug level
		/// <para>  The best is set to specific methods via:   method:value|method:value|method:value.</para>
		/// <para>  Valid methods are:</para>
		/// <para>    compile, pack, gitversion</para>
		/// <para>  Valid values are:</para>
		/// <para>    debug, warn, info</para></param>
		/// <returns></returns>
		public static int Main(string rootdir = "", 
		                       string deployto = "test", 
		                       string compileconfig = "", 
		                       bool faststart = false, 
		                       string verbosity = "", 
		                       bool interactive = true,
		                       bool skipnuget = false,
		                       bool info = false) {
			CISession ciSession = new CISession();

			try {
				Console.SetWindowSize(130,34);

				Misc.WriteAppHeader();

				// We process thru defaults, at the end if interactive is on, then we
				// prompt user for changes / confirmation

				

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
					DetermineCompileConfiguration(ciSession);
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
				slugCI.Startup();

				if (interactive)
					Menu(ciSession, slugCI);


				// If user wanted info then display it and exit.
				if ( info && !interactive) {
					slugCI.DisplayInfo();
					return 0;
				}


				// Begin Executing
				slugCI.Execute();

				return 0;
			}
			catch ( Exception e ) {
				if (ciSession.GitProcessor != null) 
					if (ciSession.GitProcessor.HasErrored)
						ciSession.GitProcessor.PrintGitHistory();
				Logger.Error(e);
			}

			return 0;
		}


		/// <summary>
		/// Sets Compile Configuration if none specified on command line, based upon the publishing target.
		/// </summary>
		/// <param name="ciSession"></param>
		private static void DetermineCompileConfiguration (CISession ciSession) {
			if (ciSession.PublishTarget == PublishTargetEnum.Production)
				ciSession.CompileConfig = "Release";
			else
				ciSession.CompileConfig = "Debug";
		}


		/// <summary>
		/// Prompt user for information and confirm...
		/// </summary>
		private static bool UserPrompting (CISession ciSession) {
			Console.Clear();

			bool keepLooping = true;
			while ( keepLooping ) {
				PromptForDeployTarget(ciSession);

				DetermineCompileConfiguration(ciSession);

				PromptForConfiguration(ciSession);


				return true;
			}

			return true;
		}


		/// <summary>
		/// Displays the Main Menu for SLUGCI
		/// </summary>
		/// <param name="ciSession"></param>
		/// <param name="slugCi"></param>
		/// <returns></returns>
		private static bool Menu (CISession ciSession, SlugCI slugCi) {
			bool keepLooping = true;
			while (keepLooping) {
				Console.WriteLine(Environment.NewLine);
				Color lineColor = Color.WhiteSmoke;
				
				
				Misc.WriteMainHeader("SlugCI Interactive Menu", new List<string>() {ciSession.Solution.Name});

				Console.WriteLine(" {0,-30}    |  {1,-35}", "Target Deploy:", ciSession.PublishTarget.ToString(), lineColor);
				Console.WriteLine(" {0,-30}    |  {1,-35}", "Compile Config:", ciSession.CompileConfig, lineColor);

				// Line 1 of Menu
				string ver = "";
				if (ciSession.ManuallySetVersion != null) ver = ciSession.ManuallySetVersion.ToString();
				Console.WriteLine(" (I)  Information about Project", Color.Yellow);

				// Line 2 of Menu
				Console.WriteLine(" (V)  Manually Set the next version [ " + ver + " ]", Color.WhiteSmoke);

				// Line 3 of Menu
				if ( ciSession.SkipNuget )
					lineColor = Color.Yellow;
				else
					lineColor = Color.WhiteSmoke;
				Console.WriteLine(" (S)  Skip Nuget Publish  [ " + ciSession.SkipNuget + " ]", lineColor);

				// Line 4 of Menu
				if ( ciSession.GitProcessor.AreUncommitedChangesOnLocalBranch ) {
					lineColor = Color.Red;
					Console.WriteLine(" (R)  Refresh Git (You have uncommitted changes on branch.  Commit and then issue this command", lineColor);
				}
				

				// Last line of Menu
				Console.Write(" (X)  Exit", Color.Red);

				Console.WriteLine();
				
				// Set Valid Keys
				List<ConsoleKey> validKeys = new List<ConsoleKey>()
				{
					ConsoleKey.I,
					ConsoleKey.V,
					ConsoleKey.R,
					ConsoleKey.S,
					ConsoleKey.X,
					ConsoleKey.Enter,
				};

				ConsoleKey answer = PromptAndGetResponse(ConsoleKey.Enter, validKeys, "Press Enter to start the Build Process  OR  Select an Item");
				if ( answer == ConsoleKey.I ) slugCi.DisplayInfo();
				else if ( answer == ConsoleKey.Enter && !ciSession.GitProcessor.AreUncommitedChangesOnLocalBranch) return true;
				else if (answer == ConsoleKey.V) ManualVersionPrompts(ciSession,slugCi);
				else if ( answer == ConsoleKey.S ) ciSession.SkipNuget = !ciSession.SkipNuget;
				else if ( answer == ConsoleKey.X ) return false;
				else if ( answer == ConsoleKey.R ) ciSession.GitProcessor.RefreshUncommittedChanges();

				Console.Clear();
			}

			return true;
		}


		private static void ManualVersionPrompts (CISession ciSession, SlugCI slugCi)
		{
			Misc.WriteMainHeader("Manually Set Version");
			Console.ForegroundColor = Color.WhiteSmoke;
			
			Console.WriteLine();
			Console.WriteLine("This allows you to manually set the primary version numbers of the application for the branch being deployed to.");
			Console.WriteLine("Version number should be in format:   #.#.#");
			Console.WriteLine("Enter x to exit, not setting the version number");
			Console.WriteLine();

			bool continueLooping = true;
			while ( continueLooping ) {
				Console.WriteLine("Enter Ver # in format #.#.#");
				string response = Console.ReadLine();
				if ( response.ToLower() == "x" ) return;

				if (slugCi.SetVersionManually(response)) continueLooping = false;
			}
		}



		/// <summary>
		/// Allow user to choose where they are deploying too.
		/// </summary>
		/// <param name="ciSession"></param>
		private static void PromptForConfiguration(CISession ciSession)
		{
			Console.Clear();
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
		private static ConsoleKey PromptAndGetResponse (ConsoleKey defaultKey,List<ConsoleKey> validKeys, string prompt = "") {
			Console.WriteLine();
			if (prompt.IsNullOrEmpty())
				Console.Write("Press the letter of your choice, or Enter to accept current value.",Color.Magenta);
			else Console.Write(prompt,Color.Magenta);
			Console.WriteLine("",Color.WhiteSmoke);
			

			bool keepLooping = true;

			ConsoleKey userSelectedConsoleKey = defaultKey;

			while ( keepLooping ) {
				ConsoleKeyInfo choice = Console.ReadKey();
				if ( validKeys.Contains(choice.Key) ) userSelectedConsoleKey = choice.Key;
				else if ( choice.Key == ConsoleKey.Enter )
					userSelectedConsoleKey = defaultKey;
				else
					continue;

				// User selected a valid item, so exit.
				keepLooping = false;
			}

			Console.ResetColor();
			return userSelectedConsoleKey;
		}



		/// <summary>
		/// Sets verbosity of components based upon verbosity setting.
		/// </summary>
		/// <param name="verbosity"></param>
		/// <param name="ciSession"></param>
		private static void SetVerbosity (string verbosity, CISession ciSession) {
			if ( verbosity.IsNullOrEmpty() ) return;
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
