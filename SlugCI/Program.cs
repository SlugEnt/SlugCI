using System;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
	
namespace SlugCI
{
	
	class Program : NukeBuild
	{
		public static int Main()
		{
			SlugBuilder slugBuilder = new SlugBuilder();

			slugBuilder.Clean();
			slugBuilder.RestoreNugetPackages();
			slugBuilder.Compile();
			slugBuilder.Pack();
/*			
			DotNetBuild(s => s.SetProjectFile(Solution)
			                  .SetConfiguration(Configuration)
			                  .SetAssemblyVersion(assemblyVer)
			                  .SetFileVersion(fileVer)
			                  .SetInformationalVersion(infoVer)
			                  .SetVerbosity(DotNetVerbosity.Minimal)
			                  .EnableNoRestore());
*/



			Console.WriteLine("Hello World!");
			return 0;
		}


	}
}
