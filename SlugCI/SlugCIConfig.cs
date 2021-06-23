using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nuke.Common.IO;
using static Nuke.Common.IO.FileSystemTasks;


namespace Slug.CI {
	/// <summary>
	/// The method used to deploy a given package.
	/// </summary>
	public enum SlugCIDeployMethod {
		/// <summary>
		/// Not deployed
		/// </summary>
		[EnumMember(Value = @"None")]
		None = 0,

		/// <summary>
		/// Is a nuget package and will be deployed to a nuget repository.
		/// </summary>
		[EnumMember(Value = @"Nuget")]
		Nuget = 1,

		/// <summary>
		/// Is copied to a deployment folder location
		/// </summary>
		[EnumMember(Value = @"Copy")]
		Copy = 2
	}


	/// <summary>
	/// Contains information about the solution and projects that SlugNuke needs in order to build and publish the projects.
	/// </summary>
	public class SlugCIConfig :IEquatable<SlugCIConfig> {
		/// <summary>
		/// This value should be updated every time this config layout changes
		/// </summary>
		public const string CONFIG_STRUCTURE_VERSION = "0.22.0";

		/// <summary>
		/// The version format of the config file.
		/// </summary>
		public string ConfigStructureVersion { get; set; }


		/// <summary>
		/// If true Code Coverage reports will be run.
		/// </summary>
		public bool UseCodeCoverage { get; set; } = true;


		/// <summary>
		/// The threshold required or else Code Coverage will throw an error.
		/// </summary>
		public short CodeCoverageThreshold { get; set; } = 90;


		/// <summary>
		/// The root folder to deploy Production  to
		/// </summary>
		public string DeployProdRoot { get; set; }

		/// <summary>
		/// The root folder to deploy Alpha to
		/// </summary>
		public string DeployAlphaRoot { get; set; }


		/// <summary>
		/// The root folder to deploy Beta to
		/// </summary>
		public string DeployBetaRoot { get; set; }


		/// <summary>
		/// The root folder to deploy Development to
		/// </summary>
		public string DeployDevRoot { get; set; }


		/// <summary>
		/// If true, it will deploy to a subfolder with the name of the Version tag - Ver#.#.#. If False, no subfolder is created.
		/// </summary>
		public bool DeployToVersionedFolder { get; set; } = true;


		/// <summary>
		/// If true the DeployToVersionedFolder uses a full SemVer, Ie. Ver#.#.#-xyz
		/// </summary>
		public bool DeployFolderUsesSemVer { get; set; } = true;


		/// <summary>
		/// If true, the name of the project (Full Namespace name) will be used with every period in the name being a new subfolder.
		/// So:  MySpace.MyApp.SubApp would be deployed to a folder DeployRoot\Prod\MySpace\MyApp\SubApp\MySpace.MyApp.SubApp\Ver#.#.#
		/// </summary>
		public bool DeployToAssemblyFolders { get; set; } = false;


		/// <summary>
		///  The Git Remote name.  Typically is Origin.  If null, we use Origin.
		/// </summary>
		public string GitRemote { get; set; } = null;


		/// <summary>
		/// The root part of the folder name for an Angular Project deployment.  For example, name is [root].[appname]
		/// </summary>
		public string AngularDeployRootName { get; set; }


		/// <summary>
		/// Projects in the solution
		/// </summary>
		public List<SlugCIProject> Projects { get; set; }


		/// <summary>
		/// All Angular Web Projects
		/// </summary>
		public List<AngularProject> AngularProjects { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public SlugCIConfig () {
			Projects = new List<SlugCIProject>();
			AngularProjects = new List<AngularProject>();
		}


		/// <summary>
		/// Returns the project with the given name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public SlugCIProject GetProjectByName (string name) {
			string value = name.ToLower();
			foreach ( SlugCIProject slugCiProject in Projects ) {
				if ( slugCiProject.Name.ToLower() == value ) return slugCiProject;
			}

			return null;
		}


		public static JsonSerializerOptions SerializerOptions () {
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonStringEnumConverter());
			options.WriteIndented = true;
			return options;
		}


		/// <summary>
		/// Validates that the DeployRoot folder based upon the current config is set to a value.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public bool IsRootFolderSpecified (PublishTargetEnum config) {
			string value ="";
			if ( config == PublishTargetEnum.Production ) value = DeployProdRoot;
			else if (config == PublishTargetEnum.Beta ) value = DeployBetaRoot;
			else if ( config == PublishTargetEnum.Alpha ) value = DeployAlphaRoot;
			else if ( config == PublishTargetEnum.Development ) value = DeployDevRoot;
			else 
				throw new ArgumentException("PublishTargetEnum:  Value was not in IF logic.  Probably means a code update needs to be done.");

			if ( value == null ) return false;
			if ( value == string.Empty ) return false;
			if ( value.Trim() == string.Empty ) return false;
			return true;
		}


		/// <summary>
		/// Determines if the root folder for the given config is set to use environment variables or is unset.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public bool IsRootFolderUsingEnvironmentVariable (PublishTargetEnum config) {
			if (config == PublishTargetEnum.Production) return (DeployProdRoot == "_");
			else if (config == PublishTargetEnum.Beta) return (DeployBetaRoot == "_");
			else if (config == PublishTargetEnum.Alpha) return (DeployAlphaRoot == "_");
			else if (config == PublishTargetEnum.Development) return (DeployDevRoot == "_");
			else
				throw new ArgumentException("PublishTargetEnum:  Value was not in IF logic.  Probably means a code update needs to be done.");
		}


		/// <summary>
		/// Creates a new Exact copy of the current SlugCIConfig object
		/// </summary>
		/// <returns></returns>
		public SlugCIConfig Copy () {
			SlugCIConfig b = new SlugCIConfig();
			b.CodeCoverageThreshold = CodeCoverageThreshold;
			b.DeployFolderUsesSemVer = DeployFolderUsesSemVer;
			b.DeployProdRoot = DeployProdRoot;
			b.DeployAlphaRoot = DeployAlphaRoot;
			b.DeployDevRoot = DeployDevRoot;
			b.DeployToAssemblyFolders = DeployToAssemblyFolders;
			b.DeployToVersionedFolder = DeployToVersionedFolder;
			b.UseCodeCoverage = UseCodeCoverage;
			b.GitRemote = GitRemote;
			b.ConfigStructureVersion = ConfigStructureVersion;

			foreach ( SlugCIProject project in Projects ) {
				b.Projects.Add(project.Copy());
			}

			foreach ( AngularProject angularProject in AngularProjects ) {
				b.AngularProjects.Add(angularProject.Copy ());
			}

			return b;
		}

		
		/// <summary>
		/// Loads the config file from a file.  Returns the config object or null if the file was not found.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static SlugCIConfig LoadFromFile (AbsolutePath fileName) {
			SlugCIConfig slugCiConfig;
			if (FileExists(fileName))
			{
				string Json = File.ReadAllText(fileName);
				slugCiConfig = JsonSerializer.Deserialize<SlugCIConfig>(Json, SlugCIConfig.SerializerOptions());
				return slugCiConfig;
			}

			return null;
		}

		#region "Equality Operators"
		/// <summary>
		/// Equals method override
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => this.Equals(obj as SlugCI);


		public bool Equals([AllowNull] SlugCIConfig b)
		{
			if (b is null)
				return false;

			// Optimization for a common success case.
			if (Object.ReferenceEquals(this, b))
				return true;

			// If run-time types are not exactly the same, return false.
			if (this.GetType() != b.GetType())
				return false;

			if ( b.CodeCoverageThreshold != CodeCoverageThreshold ) return false;
			if ( b.DeployFolderUsesSemVer != DeployFolderUsesSemVer ) return false;
			if ( b.DeployToAssemblyFolders != DeployToAssemblyFolders ) return false;
			if ( b.DeployToVersionedFolder != DeployToVersionedFolder ) return false;
			if ( b.DeployAlphaRoot != DeployAlphaRoot ) return false;
			if ( b.DeployProdRoot != DeployProdRoot ) return false;
			if ( b.DeployDevRoot != DeployDevRoot ) return false;
			if ( b.UseCodeCoverage != UseCodeCoverage ) return false;
			if ( b.Projects.Count != Projects.Count ) return false;
			if ( b.GitRemote != GitRemote ) return false;
			if ( b.ConfigStructureVersion != ConfigStructureVersion ) return false;

			// Loop thru projects looking for complete matches
			foreach ( SlugCIProject project in Projects ) {
				// Find project in other object
				SlugCIProject c =  b.Projects.Find(p => p.Name == project.Name);
				if ( c == null ) return false;
				if ( c != project ) return false;
			}


			// Loop thru Angular Projects looking for complete matches
			foreach (AngularProject project in AngularProjects)
			{
				// Find project in other object
				AngularProject c = b.AngularProjects.Find(p => p.Name == project.Name);
				if (c == null) return false;
				if (c != project) return false;
			}
			return true;
		}


		public static bool operator ==(SlugCIConfig lhs, SlugCIConfig rhs)
		{
			if (lhs is null)
			{
				if (rhs is null)
				{
					return true;
				}

				// Only the left side is null.
				return false;
			}
			// Equals handles case of null on right side.
			return lhs.Equals(rhs);
		}

		public static bool operator !=(SlugCIConfig lhs, SlugCIConfig rhs) => !(lhs == rhs);

		#endregion
	}

}