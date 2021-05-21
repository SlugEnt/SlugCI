using System;
using System.Collections.Generic;
using System.Text;

namespace Slug.CI
{
	/// <summary>
	/// Where we are publishing the solution to.
	/// </summary>
	public enum PublishTargetEnum
	{
		/// <summary>
		/// Publish to Development
		/// </summary>
		Development = 20,

		/// <summary>
		/// Publish to Alpha-Test
		/// </summary>
		Alpha = 100,


		/// <summary>
		/// Publish to Beta-Test
		/// </summary>
		Beta = 175,


		/// <summary>
		/// Publish to Production
		/// </summary>
		Production = 200
	}
}
