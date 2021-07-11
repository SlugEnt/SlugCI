using System;
using Nuke.Common.Utilities;
using Semver;

namespace Slug.CI
{
	/// <summary>
	/// Class that stores all version attributes for a given .Net Project.
	/// </summary>
	public class VersionInfo {
		public SemVersion SemVersion { get; private set; }

		public string SemVersionAsString { get; private set; }

		public string AssemblyVersion { get; private set; }

		public string FileVersion { get; private set; }

		public string InformationalVersion { get; private set; }

		public SemVersionPreRelease PreRelease { get; set; }

		/// <summary>
		/// NPM Version.  NPM only uses pure SemVer, no pre-release information
		/// </summary>
		public string NPMVersion { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="semVersion"></param>
		/// <param name="commitHash"></param>
		public VersionInfo (SemVersion semVersion, string commitHash) {
			// Convert the semVersion into other .Net Version values
			SemVersion = semVersion;
			SemVersionAsString = semVersion.ToString();

			string prefix = semVersion.Major + "." + semVersion.Minor + "." + semVersion.Patch + ".";
			
			// Assembly is the semVersion with the 4th value always a zero.
			AssemblyVersion = prefix + "0";

			// File Version
			if ( ! String.IsNullOrEmpty(semVersion.Prerelease) ) {
				PreRelease = new SemVersionPreRelease(semVersion.Prerelease);
				int fileNum = 1;
				if ( PreRelease.ReleaseType == "alpha" )
					fileNum = 100000;
				else if ( PreRelease.ReleaseType == "beta" ) fileNum = 200000;
				fileNum += PreRelease.ReleaseNumber;
				FileVersion = prefix + fileNum;
			}
			else
				FileVersion = prefix + "1";


			InformationalVersion = SemVersionAsString + ".g" + commitHash;

			NPMVersion = semVersion.Major + "." + semVersion.Minor + "." + semVersion.Patch;
		}
	}
}
