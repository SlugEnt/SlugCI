using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;
using Semver;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;
using Console = Colorful.Console;
using Logger = Nuke.Common.Logger;

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
		/// <param name="failedtestsok">If true, unit tests that fail do not stop the build from continuing</param>
		/// <param name="verbosity">Sets the verbosity of command output.  You can set value for all commands or just certain commands.  Be careful with all, it can generate a LOT of output on debug level
		/// <param name="setup">Perform initial setup of a repository</param>
		/// <param name="skipangular">Skips the Angular Build</param>
		/// <param name="skiptests">Will skip unit testing completely</param>
		/// <para>  The best is set to specific methods via:   method:value|method:value|method:value.</para>
		/// <para>  Valid methods are:</para>
		/// <para>    compile, pack, gitversion</para>
		/// <para>  Valid values are:</para>
		/// <para>    debug, warn, info</para></param>
		/// <returns></returns>
		public static async Task<int> Main(string rootdir = "", 
		                                   string deployto = "test", 
		                                   string compileconfig = "", 
		                                   bool faststart = false, 
		                                   string verbosity = "", 
		                                   bool interactive = true,
		                                   bool skipnuget = false,
		                                   bool failedtestsok = false,
		                                   bool info = false,
		                                   bool setup = false,
		                                   bool skipangular = false,
		                                   bool skiptests = false) {
			CISession ciSession = new CISession();

			Logger.SetOutputSink(CISession.OutputSink);

			try {
				Console.SetWindowSize(130,34);
				string slugCIVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				Misc.WriteAppHeader(new List<string>() {"SlugCI Version: " + slugCIVersion});


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


				// Skip Angular Web Build
				ciSession.SkipAngularBuild = skipangular;


				// Failed Unit tests are ok?
				ciSession.FailedUnitTestsOkay = failedtestsok;


				// Setup mode
				ciSession.IsInSetupMode = setup;

				// Skip unit tests
				ciSession.SkipTests = skiptests;


				// Perform Validation 
				ValidateDependencies validation = new ValidateDependencies(ciSession);
				if (!validation.Validate()) throw new ApplicationException("One or more required features is missing from this pc.");

				// Create the SlugCI which is main processing class.
				SlugCI slugCI = new SlugCI(ciSession);
				Task slugCITask = Task.Run(slugCI.StartupAsync);

				slugCITask.Wait();

				if ( !slugCI.IsReady ) {
					Colorful.Console.WriteLine("SlugCI Initializer is not ready to continue processing the solution.  Exiting...", Color.Red);
					return 1;
				}


				// Interactive mode....
				if (interactive)
					UserPrompting(ciSession);


				
				slugCI.WriteLines();

				if (interactive)
					if ( !Menu(ciSession, slugCI) )
						return 1;


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
			PromptForDeployTarget(ciSession);

			DetermineCompileConfiguration(ciSession);

			PromptForConfiguration(ciSession);


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
			
			// Get some variables that are expensive to get, just once.
			string versionAlpha = ciSession.GitProcessor.GetMostRecentVersionTagOfBranch("alpha").ToString();
			string versionBeta = ciSession.GitProcessor.GetMostRecentVersionTagOfBranch("beta").ToString();
			string versionMain = ciSession.GitBranches[ciSession.GitProcessor.MainBranchName].LatestSemVersionOnBranch.ToString();

			while ( keepLooping ) {
				Console.WriteLine(Environment.NewLine);
				Color lineColor = Color.WhiteSmoke;

				// Display Git Info / Versions of project
				string versionPreReleaseName = "alpha";

				// Get most recent Version Tag for the desired branch type


				Misc.WriteSubHeader("Git Project Information");
				Console.WriteLine(" {0,-25}  |  {1,-34}", "Current Branch", ciSession.GitProcessor.CurrentBranch);
				Console.WriteLine(" {0,-25}  |  {1,-20}", "Main Branch Name", ciSession.GitProcessor.MainBranchName);
				Console.WriteLine(" {0,-25}  |  {1,-20}", "Main Branch Version #", versionMain);

				Console.WriteLine(" {0,-25}  |  {1,-20}", "Alpha Branch Version #", versionAlpha);
				Console.WriteLine(" {0,-25}  |  {1,-20}", "Beta Branch Version #", versionBeta);


				Misc.WriteMainHeader("SlugCI Interactive Menu", new List<string>() {ciSession.Solution.Name});

				Console.WriteLine(" {0,-30}    |  {1,-35}", "Target Deploy:", ciSession.PublishTarget.ToString(), lineColor);
				Console.WriteLine(" {0,-30}    |  {1,-35}", "Compile Config:", ciSession.CompileConfig, lineColor);

				// Menu Item
				Console.WriteLine(" (I)  Information about Project", Color.Yellow);

				// Menu Item
				string ver = "";
				if ( ciSession.ManuallySetVersion != null ) 
					ver = ciSession.ManuallySetVersion.ToString();
				lineColor = ver != string.Empty ? Color.Yellow : Color.WhiteSmoke;
				Console.WriteLine(" (V)  Manually Set the next version [ " + ver + " ]", lineColor);
				Console.WriteLine(" (9)  Show Next Version #", Color.WhiteSmoke);
				Console.WriteLine();

				// Menu Item
				Console.WriteLine(" (C)  Cleanup Git Repo");
				
				// Menu Item
				if ( ciSession.SkipNuget )
					lineColor = Color.Yellow;
				else
					lineColor = Color.WhiteSmoke;
				Console.WriteLine(" (S)  Skip Nuget Publish  [ " + ciSession.SkipNuget + " ]", lineColor);

				// Menu Item
				if (ciSession.SkipAngularBuild)
					lineColor = Color.Yellow;
				else
					lineColor = Color.WhiteSmoke;
				Console.WriteLine(" (A)  Skip Angular Build & Publish  [ " + ciSession.SkipAngularBuild + " ]", lineColor);

				// Menu Item
				if (ciSession.SkipTests)
					lineColor = Color.Yellow;
				else
					lineColor = Color.WhiteSmoke;
				Console.WriteLine(" (T)  Skip All Test Runs  [ " + ciSession.SkipTests + " ]", lineColor);

				// Menu Item
				if ( ciSession.FailedUnitTestsOkay )
					lineColor = Color.Yellow;
				else
					lineColor = Color.WhiteSmoke;
				Console.WriteLine(" (U)  Failed Unit Tests are okay - continue building:  {0}", ciSession.FailedUnitTestsOkay ,lineColor);

				// Menu Item
				if ( ciSession.GitProcessor.AreUncommitedChangesOnLocalBranch ) {
					lineColor = Color.Red;
					Console.WriteLine(" (R)  Refresh Git (You have uncommitted changes on branch).  Commit and then issue this command", lineColor);
				}
				

				// Last line of Menu
				Console.Write(" (X)  Exit", Color.Red);

				Console.WriteLine();
				
				// Set Valid Keys
				List<ConsoleKey> validKeys = new List<ConsoleKey>()
				{
					ConsoleKey.A,
					ConsoleKey.I,
					ConsoleKey.C,
					ConsoleKey.V,
					ConsoleKey.R,
					ConsoleKey.S,
					ConsoleKey.T,
					ConsoleKey.U,
					ConsoleKey.X,
					ConsoleKey.D9,
					ConsoleKey.Enter,
				};

				ConsoleKey answer = PromptAndGetResponse(ConsoleKey.Enter, validKeys, "Press Enter to start the Build Process  OR  Select an Item");
				if ( answer == ConsoleKey.I ) slugCi.DisplayInfo();
				else if ( answer == ConsoleKey.Enter && !ciSession.GitProcessor.AreUncommitedChangesOnLocalBranch) return true;
				else if (answer == ConsoleKey.V) ManualVersionPrompts(ciSession,slugCi);
				else if ( answer == ConsoleKey.S ) ciSession.SkipNuget = !ciSession.SkipNuget;
				else if ( answer == ConsoleKey.A ) ciSession.SkipAngularBuild = !ciSession.SkipAngularBuild;
				else if ( answer == ConsoleKey.X ) return false;
				else if ( answer == ConsoleKey.R ) ciSession.GitProcessor.RefreshUncommittedChanges();
				else if ( answer == ConsoleKey.T ) ciSession.SkipTests = true;
				else if ( answer == ConsoleKey.U ) ciSession.FailedUnitTestsOkay = true;
				else if ( answer == ConsoleKey.D9 ) {
					BuildStage_CalcVersion calcVersion = new BuildStage_CalcVersion(ciSession);
					calcVersion.Execute();
					Console.WriteLine("{0}{0}", Environment.NewLine);
					Console.WriteLine("Next version will be:  ", Color.DarkCyan);
					Console.WriteLine("  Assembly Version:       {0}", ciSession.VersionInfo.AssemblyVersion);
					Console.WriteLine("  File Version:           {0}", ciSession.VersionInfo.FileVersion);
					Console.WriteLine("  Informational Version:  {0}", ciSession.VersionInfo.InformationalVersion);
					Console.WriteLine("  SemVersion:             {0}", ciSession.VersionInfo.SemVersionAsString);
					Console.WriteLine("  NPM Version:            {0}", ciSession.VersionInfo.NPMVersion);
					Console.WriteLine("{0}{0}Press any key to return to menu", Environment.NewLine);
					Console.ReadKey();
					Console.Clear();
				}
				else if ( answer == ConsoleKey.C ) {
					BuildStage_GitCleanup gitCleanup = new BuildStage_GitCleanup(ciSession);
					if (!gitCleanup.Execute() ) Console.WriteLine("Git Cleanup Failed.");
					else Console.WriteLine("Git Cleanup Success!");
					Console.WriteLine("Press any key to continue");
					Console.ReadKey();
				}

				Console.Clear();
			}

			return true;
		}



		/// <summary>
		/// Allows user to change the next version of the app - manually.
		/// </summary>
		/// <param name="ciSession"></param>
		/// <param name="slugCi"></param>
		private static void ManualVersionPrompts (CISession ciSession, SlugCI slugCi)
		{
			Misc.WriteMainHeader("Set Version Override");
			Console.ForegroundColor = Color.WhiteSmoke;
			
			Console.WriteLine();
			Console.WriteLine("This allows you to manually set the primary version numbers of the application for the branch being deployed to.");
			Console.WriteLine();
			Console.WriteLine("You are currently targeting a build to: {0}",ciSession.PublishTarget.ToString());

			bool continueLooping = true;
			PublishTargetEnum target = ciSession.PublishTarget;
			string branchName = target switch
			{
				PublishTargetEnum.Alpha => "alpha",
				PublishTargetEnum.Beta => "beta",
				PublishTargetEnum.Production => ciSession.GitProcessor.MainBranchName,
			};

			SemVersion currentMaxVersion = ciSession.GitProcessor.GetMostRecentVersionTagOfBranch(branchName);
			SemVersion newManualVersion = new SemVersion(0,0,0);

			Console.WriteLine("{0}The latest version on this Branch is: {1}", Environment.NewLine, currentMaxVersion);
			if ( target == PublishTargetEnum.Production ) {
				Console.WriteLine("  (1) To bump the Major version number from {0} to {1}", currentMaxVersion.Major, currentMaxVersion.Major + 1);
				Console.WriteLine("  (2) To bump the Minor version number from {0} to {1}", currentMaxVersion.Minor,currentMaxVersion.Minor + 1);
				Console.WriteLine("  (3) To bump the Patch number from {0} to {1}", currentMaxVersion.Patch, currentMaxVersion.Patch + 1);
				Console.WriteLine( "  (9) To change all 3 components at once.");
				while ( continueLooping ) {
					 ConsoleKeyInfo keyInfo = Console.ReadKey();
					 if ( keyInfo.Key == ConsoleKey.D3 )
						 newManualVersion = new SemVersion(currentMaxVersion.Major, currentMaxVersion.Minor, currentMaxVersion.Patch + 1);
					 else if ( keyInfo.Key == ConsoleKey.D2 )
						 newManualVersion = new SemVersion(currentMaxVersion.Major, currentMaxVersion.Minor + 1, 0);
					 else if ( keyInfo.Key == ConsoleKey.D1 )
						newManualVersion = new SemVersion(currentMaxVersion.Major + 1, 0, 0);
					 else if ( keyInfo.Key == ConsoleKey.D9 ) {
						Console.WriteLine("Enter X to exit without changing version  OR  enter Version number in format #.#.#");
						string manVer = Console.ReadLine();
						if ( manVer == "x" || manVer == "X" ) return;

						// Change Version
						if ( slugCi.SetVersionManually(manVer) ) continueLooping = false;
					 }

					else
						continue;
					break;
				 }

				 Console.WriteLine("{0} Y/N?  Do you want to set the version for branch {1} to version # {2}",Environment.NewLine,branchName,newManualVersion.ToString());
				 while ( true ) {
					 ConsoleKeyInfo keyInfoPYN = Console.ReadKey();
					 if ( keyInfoPYN.Key == ConsoleKey.Y ) {
						 ciSession.ManuallySetVersion = newManualVersion;
						 return;
					 }
					 else if ( keyInfoPYN.Key == ConsoleKey.N ) return;
				 }
			}

			// Alpha / Beta branch
			else {
				SemVersionPreRelease svpr = new SemVersionPreRelease(currentMaxVersion.Prerelease);
				Console.WriteLine("  (1) To bump the Major version number from {0} to {1}", currentMaxVersion.Major, currentMaxVersion.Major + 1);
				Console.WriteLine("  (2) To bump the Minor version number from {0} to {1}", currentMaxVersion.Minor, currentMaxVersion.Minor + 1);
				Console.WriteLine("  (3) To bump the Patch number from {0} to {1}", currentMaxVersion.Patch, currentMaxVersion.Patch + 1);
				Console.WriteLine("  (4) To bump the pre-release number from {0} to {1}",svpr.ReleaseNumber,svpr.ReleaseNumber+1);
				Console.WriteLine("  (9) To change all 3 components at once.");

				while (continueLooping)
				{
					ConsoleKeyInfo keyInfo = Console.ReadKey();
					if ( keyInfo.Key == ConsoleKey.D3 ) {
						newManualVersion = new SemVersion(currentMaxVersion.Major, currentMaxVersion.Minor, currentMaxVersion.Patch + 1);
						svpr =new SemVersionPreRelease(branchName,0,IncrementTypeEnum.Patch);
					}
					else if ( keyInfo.Key == ConsoleKey.D2 ) {
						newManualVersion = new SemVersion(currentMaxVersion.Major, currentMaxVersion.Minor + 1, 0);
						svpr = new SemVersionPreRelease(branchName, 0, IncrementTypeEnum.Minor);
						//svpr.BumpMinor();
					}
					else if ( keyInfo.Key == ConsoleKey.D1 ) {
						newManualVersion = new SemVersion(currentMaxVersion.Major + 1, 0, 0);
						svpr = new SemVersionPreRelease(branchName, 0, IncrementTypeEnum.Major);
						//svpr.BumpMajor();
					}
					else if ( keyInfo.Key == ConsoleKey.D4 ) {
						newManualVersion = currentMaxVersion;
						svpr.BumpVersion();
					}
					else if ( keyInfo.Key == ConsoleKey.D9 ) {
						Console.WriteLine("Enter X to exit without changing version  OR  enter Version number in format #.#.#");
						string manVer = Console.ReadLine();
						if ( manVer == "x" || manVer == "X" ) return;
						if ( !SemVersion.TryParse(manVer, out SemVersion newVer) ) continue;
						svpr = new SemVersionPreRelease(branchName,0,IncrementTypeEnum.None);

						newManualVersion = new SemVersion(newVer.Major,newVer.Minor,newVer.Patch,svpr.ToString());
					}
					else
						continue;
					break;
				}

				newManualVersion = new SemVersion(newManualVersion.Major,newManualVersion.Minor,newManualVersion.Patch,svpr.Tag());
				
				Console.WriteLine("{0}Y/N?  Do you want to set the version for branch {1} to version # {2}", Environment.NewLine, branchName, newManualVersion.ToString());
				while (true)
				{
					ConsoleKeyInfo keyInfoPYN = Console.ReadKey();
					if (keyInfoPYN.Key == ConsoleKey.Y)
					{
						ciSession.ManuallySetVersion = newManualVersion;
						return;
					}
					else if (keyInfoPYN.Key == ConsoleKey.N) return;
				}

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
			if (String.IsNullOrEmpty(prompt))
				Console.Write("Press the letter of your choice, or Enter to accept current value.",Color.Magenta);
			else Console.Write(prompt,Color.Magenta);
			Console.WriteLine("",Color.WhiteSmoke);
			

			bool keepLooping = true;

			ConsoleKey userSelectedConsoleKey = defaultKey;

			// Flush Keyboard buffer
			while ( Console.KeyAvailable ) Console.ReadKey();

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
			if ( String.IsNullOrEmpty(verbosity) ) return;
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
