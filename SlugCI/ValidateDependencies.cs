using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Drawing;
using System.Text;
using Newtonsoft.Json;
using Nuke.Common.Tooling;
using Slug.CI.NukeClasses;
using YamlDotNet.Serialization.Schemas;
using Console = Colorful.Console;


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
