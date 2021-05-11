using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic.CompilerServices;
using Nuke.Common.ProjectModel;

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
		/// Used to track if there were any changes to this config file.
		/// </summary>
		public bool HasChanged { get; set; }


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
		/// The root folder to deploy Test to
		/// </summary>
		public string DeployTestRoot { get; set; }


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
		/// Projects in the solution
		/// </summary>
		public List<SlugCIProject> Projects { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public SlugCIConfig () {
			Projects = new List<SlugCIProject>();
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
			else if ( config == PublishTargetEnum.Testing ) value = DeployTestRoot;
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
		public bool IsRootFolderUsingEnvironmentVariable (Configuration config) {
			if ( config == "Release" )
				return (DeployProdRoot == "_");
			else
				return (DeployTestRoot == "_");
		}


		/// <summary>
		/// Sets the given deployment folder based upon configuration to use Environment Variables
		/// </summary>
		/// <param name="config"></param>
		public void SetRootFolderToUseEnvironmentVariable (Configuration config) {
			if ( config == "Release" )
				DeployProdRoot = "_";
			else
				DeployTestRoot = "_";
		}

		/*
		/// <summary>
		/// Checks to ensure that if any of the projects have a Deploy method of Copy that the DeployRoot folders are specified.
		/// </summary>
		/// <returns></returns>
		public bool CheckRootFolders () {
			bool hasCopyMethod = false;

			foreach ( Project project in Projects ) {
				if ( project.Deploy == SlugCIDeployMethod.Copy ) hasCopyMethod = true;
			}

			// If no projects require a root folder then it is ok.
			if ( !hasCopyMethod ) return true;

			// Ensure Deploy Roots have values if at least one of the projects has a deploy method of Copy
			for (int i = 0; i< 2; i++ ) {
				Configuration config;

				if (i == 0 ) {
					config = Configuration.Release;
				}
				else {
					config = Configuration.Debug;
				}

				if (!IsRootFolderSpecified(config))
				{
					Console.WriteLine("There are 1 or more projects with a Deploy method of Copy, but no Deploy Root folders have been specified.");
					return false;
				}
			}

			return true;
		}
		*/


		/// <summary>
		/// Creates a new Exact copy of the current SlugCIConfig object
		/// </summary>
		/// <returns></returns>
		public SlugCIConfig Copy () {
			SlugCIConfig b = new SlugCIConfig();
			b.CodeCoverageThreshold = CodeCoverageThreshold;
			b.DeployFolderUsesSemVer = DeployFolderUsesSemVer;
			b.DeployProdRoot = DeployProdRoot;
			b.DeployTestRoot = DeployTestRoot;
			b.DeployDevRoot = DeployDevRoot;
			b.DeployToAssemblyFolders = DeployToAssemblyFolders;
			b.DeployToVersionedFolder = DeployToVersionedFolder;
			b.UseCodeCoverage = UseCodeCoverage;
			foreach ( SlugCIProject project in Projects ) {
				b.Projects.Add(project.Copy());
			}

			return b;
		}

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
			if ( b.DeployTestRoot != DeployTestRoot ) return false;
			if ( b.DeployProdRoot != DeployProdRoot ) return false;
			if ( b.DeployDevRoot != DeployDevRoot ) return false;
			if ( b.UseCodeCoverage != UseCodeCoverage ) return false;
			if ( b.Projects.Count != Projects.Count ) return false;


			// Loop thru projects looking for complete matches
			bool projectsEqual = true;
			foreach ( SlugCIProject project in Projects ) {
				// Find project in other object
				SlugCIProject c =  b.Projects.Find(p => p.Name == project.Name);
				if ( c == null ) return false;
				if ( c != project ) return false;
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
	}

}