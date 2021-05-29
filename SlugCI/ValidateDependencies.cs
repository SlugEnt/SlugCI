using System.Collections.Generic;
using Slug.CI.NukeClasses;


namespace Slug.CI
{
	public class ValidateDependencies {
		private CISession CISession { get; set; }


		public ValidateDependencies (CISession ciSession) {
			CISession = ciSession;
		}


		public bool Validate () {
			Misc.WriteMainHeader("Validate:", new List<string>() {"Confirm dependencies are installed and working"});

			bool success = true;
			
			return success;
		}
		
	}
}
