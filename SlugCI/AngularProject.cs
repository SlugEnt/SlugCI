using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Slug.CI
{
	/// <summary>
	/// An angular project config for SlugCIProject
	/// </summary>
	public class AngularProject
	{
		public string Name { get; set; }


		/// <summary>
		/// The results of the build process for this project.
		/// </summary>
		[JsonIgnore]
		public PublishResultRecord Results { get; set; }


		public AngularProject () {}

		public AngularProject (string name) { Name = name; }



		// <summary>
		/// Returns an exact copy of the current project
		/// </summary>
		/// <returns></returns>
		public AngularProject Copy()
		{
			AngularProject b = new AngularProject();
			b.Name = Name;
			return b;
		}

		public bool Equals([AllowNull] AngularProject b)
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

			return true;
		}


		public static bool operator ==(AngularProject lhs, AngularProject rhs)
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

		public static bool operator !=(AngularProject lhs, AngularProject rhs) => !(lhs == rhs);

	}
}
