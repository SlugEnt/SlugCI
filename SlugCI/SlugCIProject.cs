using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Nuke.Common.ProjectModel;

namespace Slug.CI
{
	/// <summary>
	/// Represents a SlugCI Config Project.  Note, this class eventually gets loaded with a reference to a Visual Studio Project.
	/// </summary>
	public class SlugCIProject
	{
		/// <summary>
		/// Name of the project
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The name of the assembly from the visual studio project file
		/// </summary>
		[JsonIgnore]
		public string AssemblyName { get; set; }


		/// <summary>
		/// The results of the build process for this project.
		/// </summary>
		[JsonIgnore]
		public PublishResultRecord Results { get; set; }

		/// <summary>
		/// What method to use to deploy the project.  Can be Copy, Nuget, None
		/// </summary>
		public SlugCIDeployMethod Deploy { get; set; }

		/// <summary>
		/// What Framework the project is...
		/// </summary>
		public string Framework { get; set; }

		/// <summary>
		/// If this is a unit test project or not
		/// </summary>
		public bool IsTestProject { get; set; }

		/// <summary>
		/// The Visual Studio project that corresponds to this object
		/// </summary>
		[JsonIgnore]
		public Project VSProject { get; set; }

		/// <summary>
		/// Print method
		/// </summary>
		/// <returns></returns>
		public override string ToString() { return Name; }


		/// <summary>
		/// Returns an exact copy of the current project
		/// </summary>
		/// <returns></returns>
		public SlugCIProject Copy()
		{
			SlugCIProject b = new SlugCIProject();
			b.AssemblyName = AssemblyName;
			b.Name = Name;
			b.Deploy = Deploy;
			b.Framework = Framework;
			b.IsTestProject = IsTestProject;
			return b;
		}


		public bool Equals([AllowNull] SlugCIProject b)
		{
			if (b is null)
				return false;

			// Optimization for a common success case.
			if (Object.ReferenceEquals(this, b))
				return true;

			// If run-time types are not exactly the same, return false.
			if (this.GetType() != b.GetType())
				return false;

			if (b.Name != Name) return false;
			if (b.Deploy != Deploy) return false;
			if (b.Framework != Framework) return false;
			if (b.IsTestProject != IsTestProject) return false;

			return true;
		}


		public static bool operator ==(SlugCIProject lhs, SlugCIProject rhs)
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

		public static bool operator !=(SlugCIProject lhs, SlugCIProject rhs) => !(lhs == rhs);

	}
}
