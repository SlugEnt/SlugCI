using System.Collections.Generic;
using Slug.CI.NukeClasses;


namespace Slug.CI
{
	/// <summary>
	/// Used to validate that dependencies are installed prior to running.
	/// </summary>
	public class ValidateDependencies {
		private CISession CISession { get; set; }


		public ValidateDependencies (CISession ciSession) {
			CISession = ciSession;
		}


		/// <summary>
		/// Validate all required dependencies are installed.
		/// </summary>
		/// <returns></returns>
		public bool Validate () {
			Misc.WriteMainHeader("Validate:", new List<string>() {"Confirm dependencies are installed and working"});

			// TODO - Check for Typewriter and NPM

			bool success = true;
			
			return success;
		}
		
	}
}
