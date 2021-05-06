using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
	
namespace Slug.CI
{
	
	class Program : NukeBuild
	{
		/// <summary>
		/// Main Entry Point
		/// </summary>
		/// <returns></returns>
		public static int Main(string rootdir="") {
			try {
				// If no RootDir specified, then set to current directory.
				if ( rootdir == string.Empty ) rootdir = Directory.GetCurrentDirectory();

				// Create the SlugCI which is main processing class.
				SlugCI slugCI = new SlugCI(rootdir);

				return 0;
				SlugBuilder slugBuilder = new SlugBuilder();

				slugBuilder.CopyCompiledProject(@"C:\temp\slugcitest", @"C:\temp\cideploy");
				return 1;
				slugBuilder.Clean();
				slugBuilder.RestoreNugetPackages();
				slugBuilder.Compile();
				slugBuilder.Test();

				//slugBuilder.Pack();
				slugBuilder.CodeCoverage();


				Console.WriteLine("Hello World!");
			}
			catch ( Exception e ) {
				Logger.Error(e);
			}

			return 0;
		}


	}
}
