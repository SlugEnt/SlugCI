using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using System.Xml.XPath;
using JetBrains.Annotations;
using Nuke.Common.ProjectModel;
using Slug.CI;
using Slug.CI.NukeClasses;
using Console = Colorful.Console;


namespace Slug.CI
{
	/// <summary>
	/// This is the main processing logic for SlugCI.
	/// </summary>
	public class SlugCI {
		public const string SLUG_CI_CONFIG_FILE = "SlugCI_Config.json";

		/// <summary>
		/// The CI Session information 
		/// </summary>
		public CISession CISession { get; private set; }




		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rootDir"></param>
		public SlugCI (CISession ciSession) {
			CISession = ciSession;
			CISession.SlugCIPath = CISession.RootDirectory / ".slugci";
			CISession.SourceDirectory = CISession.RootDirectory / "src";
			CISession.TestsDirectory = CISession.RootDirectory / "tests";
			CISession.OutputDirectory = CISession.RootDirectory / "artifacts";
			CISession.SlugCIFileName = CISession.SlugCIPath / SLUG_CI_CONFIG_FILE;


			// Location Solution and set solution related variables
			LoadSolutionInfo();

			// Ensure Solution is in SlugCI format. If not migrate it.
			ConvertToSlugCI converter = new ConvertToSlugCI(CISession);
			if ( !converter.IsInSlugCIFormat ) {
				ControlFlow.Assert(converter.IsInSlugCIFormat,"The solution is not in the proper SlugCI format.  This should be something that is automatically done.  Obviously something went wrong.");
			}


			// Reload solution info if it was moved.
			if (converter.SolutionWasMoved) 
				LoadSolutionInfo();


			CheckForEnvironmentVariables();
		}


		private void LoadSolutionInfo () {
			List<string> solutionFiles = SearchForSolutionFile(CISession.RootDirectory.ToString(), ".sln");
			ControlFlow.Assert(solutionFiles.Count != 0, "Unable to find the solution file");
			ControlFlow.Assert(solutionFiles.Count == 1, "Found more than 1 solution file under the root directory -  - We can only work with 1 solution file." + CISession.RootDirectory.ToString());
			CISession.SolutionFileName = solutionFiles[0];
			CISession.Solution = SolutionSerializer.DeserializeFromFile<Solution>(CISession.SolutionFileName);
			CISession.SolutionPath = CISession.Solution.Directory;

		}


		[CanBeNull]
		private static AbsolutePath GetSlugCIExecutingDirectory()
		{
			var entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly == null || entryAssembly.GetTypes().All(x => !x.IsSubclassOf(typeof(SlugCI))))
				return null;

			return (AbsolutePath)Path.GetDirectoryName(entryAssembly.Location).NotNull();
		}



		/// <summary>
		/// Runs the SlugCI Process
		/// </summary>
		public void Execute () {
			SlugBuilder slugBuilder = new SlugBuilder(CISession);

			slugBuilder.CopyCompiledProject(@"C:\temp\slugcitest", @"C:\temp\cideploy");
			return;
			slugBuilder.Test();

			//slugBuilder.Pack();
			slugBuilder.CodeCoverage();

		}

		/// <summary>
		/// Checks to ensure environment variables are set.
		/// </summary>
		/// <returns></returns>
		private bool CheckForEnvironmentVariables()
		{
			List<string> requiredEnvironmentVariables = new List<string>()
			{
				"SLUGCI_DEPLOY_PROD",
				"SLUGCI_DEPLOY_TEST",
				"SLUGCI_DEPLOY_DEV",
				"GITVERSION_EXE",		// GitVersion Tooling requires this
			};

			List<string> missingEnvironmentVariables = new List<string>();

			foreach (string name in requiredEnvironmentVariables)
			{
				string result = Environment.GetEnvironmentVariable(name);
				if (result == null) missingEnvironmentVariables.Add(name);
			}

			if (missingEnvironmentVariables.Count == 0)
			{
				Console.WriteLine("All required environment variables found", Color.Green);
				return true;
			}

			Console.WriteLine("Some environment variables are missing.  These may or may not be required.", Color.Yellow);
			foreach (string item in missingEnvironmentVariables) Console.WriteLine("  -->  " + item);
			return false;
		}



		/// <summary>
		/// Looks for the .sln file in the current folder and all subdirectories.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="searchTerm"></param>
		/// <returns></returns>
		public static List<string> SearchForSolutionFile(string root, string searchTerm)
		{
			List<string> files = new List<string>();

			foreach (var file in Directory.EnumerateFiles(root).Where(m => m.EndsWith(searchTerm)))
			{
				files.Add(file);
			}
			foreach (var subDir in Directory.EnumerateDirectories(root))
			{
				try
				{
					files.AddRange(SearchForSolutionFile(subDir, searchTerm));
				}
				catch (UnauthorizedAccessException)
				{
					// ...
				}
			}

			return files;
		}



		/// <summary>
		/// Pre-Processing that must occur for majority of the targets to work.
		/// </summary>

	}
}
