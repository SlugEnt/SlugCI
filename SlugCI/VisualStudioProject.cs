using System.Collections.Generic;
using Nuke.Common.IO;

namespace Slug.CI
{
	/// <summary>
	/// Represents a Visual Studio Project for the Setup Target Functionality
	/// </summary>
	public class VisualStudioProject {
		public string Name { get; set; }
		public string Namecsproj { get; set; }
		public AbsolutePath OriginalPath { get; set; }
		public AbsolutePath NewPath { get; set; }
		public bool IsTestProject { get; set; }
		public SlugCIDeployMethod SlugCIDeploymentMethod { get; set; }
		public List<string> Frameworks { get; set; } = new List<string>();

		// Library or Exe
		public string ProjectType { get; set; }

		public VisualStudioProject () {}

		// True if the project is a .Net Tool
		public bool IsToolPackage { get; set; }

		public string ToolName { get; set; }


		/// <summary>
		/// Construct from a Visual Studio Project from Solution.
		/// </summary>
		/// <param name="nukeProject"></param>
		public VisualStudioProject (Nuke.Common.ProjectModel.Project nukeProject) {
			Name = nukeProject.Name;
			Namecsproj = Name + ".csproj";
		}
	}
}
