using Nuke.Common;
using System;


namespace Slug.CI
{
	/// <summary>
	/// Determines the maximum type of update that occurred in an alpha or beta release.  
	/// </summary>
	public enum IncrementTypeEnum {
		/// <summary>
		/// No Increment Type
		/// </summary>
		None = 0,

		/// <summary>
		/// This release is a Patch level increase
		/// </summary>
		Patch = 10,

		/// <summary>
		/// This release is a Minor level increase
		/// </summary>
		Minor = 20,

		/// <summary>
		/// This is a Major level increase
		/// </summary>
		Major = 30
	}


	public class SemVersionPreRelease {
		public const string SEMPRE_PATCH = "a";
		public const string SEMPRE_MINOR = "b";
		public const string SEMPRE_MAJOR = "c";


		/// <summary>
		///  Whether Alpha or Beta
		/// </summary>
		public string ReleaseType { get; private set; }

		/// <summary>
		/// The numeric increment counter
		/// </summary>
		public int ReleaseNumber { get; private set; }

		/// <summary>
		/// The type of increment.
		/// </summary>
		public IncrementTypeEnum IncrementType { get; private set; }


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="preReleaseTag">An already existing pre-release tag in string format</param>
		public SemVersionPreRelease (string preReleaseTag) {
			// The normal should be alpha-0002, but there may be some that have alpha.0002, we accept either.
			int index = preReleaseTag.IndexOf('-');
			if ( index == -1 ) {
				index = preReleaseTag.IndexOf('.');
				if (index == -1)
					throw new ArgumentException("Unable to find the . in the prerelease portion of the semanticVersion [" + preReleaseTag + "]");
			}

			string trailer = preReleaseTag.Substring(++index);
			
			// If the trailer ends with a alphabetic character then its telling us its increment Type
			if ( trailer.EndsWith(SEMPRE_PATCH) ) IncrementType = IncrementTypeEnum.Patch;
			else if ( trailer.EndsWith(SEMPRE_MINOR) ) IncrementType = IncrementTypeEnum.Minor;
			else if ( trailer.EndsWith(SEMPRE_MAJOR) )
				IncrementType = IncrementTypeEnum.Major;
			else
				IncrementType = IncrementTypeEnum.None;

			if ( IncrementType != IncrementTypeEnum.None ) trailer = trailer.Substring(0, trailer.Length - 1);

			bool isInt = int.TryParse(trailer, out int value);
			
			ControlFlow.Assert(isInt, "PreRelease tag of [" + preReleaseTag + "] does not contain an integer component after the .");
			ReleaseNumber = value;
			ReleaseType = preReleaseTag.Substring(0, --index);
			IsValidReleaseType(ReleaseType);
		}


		/// <summary>
		/// Constructs a SemVersionPreRelease tag
		/// </summary>
		/// <param name="releaseType">Either Alpha or Beta typically</param>
		/// <param name="releaseNumber"></param>
		/// <param name="incrementType"></param>
		public SemVersionPreRelease (string releaseType, int releaseNumber, IncrementTypeEnum incrementType) {
			IsValidReleaseType(releaseType);
			ReleaseType = releaseType;
			ReleaseNumber = releaseNumber;
			IncrementType = incrementType;
		}


		/// <summary>
		/// Determines if a release type is valid.  Throws exception if not.
		/// </summary>
		/// <param name="releaseType"></param>
		/// <returns></returns>
		internal void IsValidReleaseType (string releaseType) {
			bool isValid = (releaseType == "alpha" || releaseType == "beta");
			ControlFlow.Assert(isValid,"Invalid ReleaseType specified [" + releaseType + "].  Can only be alpha or beta.");
		}


		/// <summary>
		/// Bumps the version by 1.
		/// </summary>
		public void BumpVersion () {
			ReleaseNumber++;
		}


		/// <summary>
		/// Increases the pre-release numeric component by 1 and sets IncrementType to Patch.
		/// </summary>
		public void BumpPatch () {
			if ( IncrementType == IncrementTypeEnum.None )
				IncrementType = IncrementTypeEnum.Patch;

			ReleaseNumber++;
		}


		/// <summary>
		/// Bumps the pre-release numeric component by 1 and sets the IncrementType to minor.
		/// </summary>
		public void BumpMinor () {
			if ( IncrementType < IncrementTypeEnum.Minor ) IncrementType = IncrementTypeEnum.Minor;
			ReleaseNumber++;
		}


		/// <summary>
		/// Bumps the pre-release numeric component by 1 and sets the IncrementType to major.
		/// </summary>
		public void BumpMajor () {
			if ( IncrementType < IncrementTypeEnum.Major ) IncrementType = IncrementTypeEnum.Major;
			ReleaseNumber++;
		}


		/// <summary>
		/// Creates the string value of the SemVersionPreRelease object
		/// </summary>
		/// <returns></returns>
		public string Tag () {
			string strIncrementType = "";
			if ( IncrementType == IncrementTypeEnum.Major ) strIncrementType = "c";
			else if ( IncrementType == IncrementTypeEnum.Minor ) strIncrementType = "b";
			else if ( IncrementType == IncrementTypeEnum.Patch ) strIncrementType = "a";

			string value = ReleaseType + "-" + ReleaseNumber.ToString("D4") + strIncrementType;
			return value;
		}

		
	}
}
