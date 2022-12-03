using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System;
using Semver;
using Slug.CI;
using Slug.CI.NukeClasses;
using Slug.CI.SlugBuildStages;

namespace Test_SlugCI {
	[TestFixture]
	public class Test_StageCalcVersion {
		/// <summary>
		/// Starting a new Alpha / Beta Branch
		/// </summary>
		[Test]
		[TestCase("alpha", "1.0.0", "0.2.4-alpha.0004", PublishTargetEnum.Alpha)]
		public void AlphaFirstBuildOfMonth (string destBranchName, string mainBranchVersion, string destBranchVersion, PublishTargetEnum publishTarget) {
			// A. Setup
			CISession ciSession = new();
			BuildStage_CalcVersion buildStageCalcVersion = new(ciSession);


			SemVersion currentMainVersion = SemVersion.Parse(mainBranchVersion);
			SemVersion currentBranchVersion = SemVersion.Parse(destBranchVersion);

			string currentBranchName = destBranchName;
			string mainBranchName = "main";
			bool useYMSchema = true;
			SemVersion expected = new(0);

			// Test
			SemVersion nextVersion =
				buildStageCalcVersion.CalculateNextVersion(currentMainVersion, currentBranchVersion, publishTarget, currentBranchName, mainBranchName,
				                                           useYMSchema);

			// Validate
			DateTime currentDateTime = DateTime.Now;
			Assert.AreEqual(currentDateTime.Year, nextVersion.Major, "A10:");
			Assert.AreEqual(currentDateTime.Month, nextVersion.Minor, "A20:");
			Assert.AreEqual(0, nextVersion.Patch, "A30:");
			Assert.AreEqual("alpha.0000", nextVersion.Prerelease, "A40:");
		}


		/// <summary>
		/// Second release in a given year/month in alpha branch
		/// </summary>
		[Test]
		[TestCase("alpha", "1.0.0", PublishTargetEnum.Alpha)]
		public void AlphaSecondBuildOfMonth (string destBranchName, string mainBranchVersion, PublishTargetEnum publishTarget) {
			// A. Setup
			DateTime currentDateTime = DateTime.Now;
			CISession ciSession = new();
			BuildStage_CalcVersion buildStageCalcVersion = new(ciSession);

			SemVersion currentMainVersion = SemVersion.Parse(mainBranchVersion);
			SemVersion currentBranchVersion = new(currentDateTime.Year, currentDateTime.Month, 0, "alpha.0000");

			string currentBranchName = destBranchName;
			string mainBranchName = "main";
			bool useYMSchema = true;


			// Test
			SemVersion nextVersion =
				buildStageCalcVersion.CalculateNextVersion(currentMainVersion, currentBranchVersion, publishTarget, currentBranchName, mainBranchName,
				                                           useYMSchema);

			// Validate

			Assert.AreEqual(currentDateTime.Year, nextVersion.Major, "A10:");
			Assert.AreEqual(currentDateTime.Month, nextVersion.Minor, "A20:");
			Assert.AreEqual(1, nextVersion.Patch, "A30:");
			Assert.AreEqual("alpha.0001", nextVersion.Prerelease, "A40:");
		}


		/// <summary>
		/// Multiple release in a given year/month in beta branch
		/// </summary>
		[Test]
		[TestCase("beta", "1.0.0", PublishTargetEnum.Beta)]
		public void BetaMultipleBuildOfMonth (string destBranchName, string mainBranchVersion, PublishTargetEnum publishTarget) {
			// A. Setup
			DateTime currentDateTime = DateTime.Now;
			CISession ciSession = new();
			BuildStage_CalcVersion buildStageCalcVersion = new(ciSession);

			SemVersion currentMainVersion = SemVersion.Parse(mainBranchVersion);
			SemVersion currentBranchVersion = new(currentDateTime.Year, currentDateTime.Month, 0, "beta.0000");

			string currentBranchName = destBranchName;
			string mainBranchName = "main";
			bool useYMSchema = true;


			// Test / Validate sequence
			SemVersion nextVersion =
				buildStageCalcVersion.CalculateNextVersion(currentMainVersion, currentBranchVersion, publishTarget, currentBranchName, mainBranchName,
				                                           useYMSchema);

			// Validate

			Assert.AreEqual(currentDateTime.Year, nextVersion.Major, "A10:");
			Assert.AreEqual(currentDateTime.Month, nextVersion.Minor, "A20:");
			Assert.AreEqual(1, nextVersion.Patch, "A30:");
			Assert.AreEqual(destBranchName + ".0001", nextVersion.Prerelease, "A40:");

			// Now increment multiple times and test+
			for ( int i = 1; i < 20; i++ ) {
				SemVersion version =
					buildStageCalcVersion.CalculateNextVersion(currentMainVersion, nextVersion, publishTarget, currentBranchName, mainBranchName, useYMSchema);
				Assert.AreEqual(currentDateTime.Year, version.Major, "A100:");
				Assert.AreEqual(currentDateTime.Month, version.Minor, "A200:");
				Assert.AreEqual(i, version.Patch, "A300:");
				string s = i.ToString();
				s = s.PadLeft(4, '0');
				s = destBranchName + "." + s;
				Assert.AreEqual(s, version.Prerelease, "A400:");
				nextVersion = version;
			}
		}


		/// <summary>
		/// Multiple release in a given year/month in beta branch
		/// </summary>
		[Test]
		[TestCase(VersionScenario.InitialAlpha)]
		[TestCase(VersionScenario.InitialBeta)]
		[TestCase(VersionScenario.InitialProd)]
		public void BetaMultipleBuildOfMonth2 (VersionScenario scenario) {
			// A. Setup
			DateTime currentDateTime = DateTime.Now;
			CISession ciSession = new();
			BuildStage_CalcVersion buildStageCalcVersion = new(ciSession);
			bool useYMSchema = true;

			VersionScenarioInfo versionScenarioInfo = VersionScenarioBuilderYM(scenario);


			// B. Test / Validate sequence
			SemVersion nextVersion = buildStageCalcVersion.CalculateNextVersion(versionScenarioInfo.MainBranch, versionScenarioInfo.CurrentBranch,
			                                                                    versionScenarioInfo.PublishTarget, versionScenarioInfo.CurrentBranchName,
			                                                                    versionScenarioInfo.MainBranchName, useYMSchema);


			// C. Validate
			Assert.AreEqual(versionScenarioInfo.ExpectedVersion, nextVersion,"A10: Versions are different");
			if ( versionScenarioInfo.PublishTarget == PublishTargetEnum.Production ) versionScenarioInfo.MainBranch = nextVersion;

			// Now increment multiple times and test+
			for ( int i = 1; i < 20; i++ ) {
				SemVersion version =
					buildStageCalcVersion.CalculateNextVersion(versionScenarioInfo.MainBranch, nextVersion, versionScenarioInfo.PublishTarget, versionScenarioInfo.CurrentBranchName, versionScenarioInfo.MainBranchName, useYMSchema);
				Assert.AreEqual(currentDateTime.Year, version.Major, "A100:");
				Assert.AreEqual(currentDateTime.Month, version.Minor, "A200:");
				Assert.AreEqual(i, version.Patch, "A300:");
				if ( versionScenarioInfo.PublishTarget != PublishTargetEnum.Production ) {
					string s = i.ToString();
					s = s.PadLeft(4, '0');
					s = versionScenarioInfo.ExpectedBranchName + "." + s;
					Assert.AreEqual(s, version.Prerelease, "A400:");
					nextVersion = version;
				}
				else {
					versionScenarioInfo.MainBranch = version;
				}

			}
		}



		/// <summary>
		/// Creates Test Data for various scenarios of testing the versioning functionality.
		/// </summary>
		/// <param name="scenario"></param>
		/// <returns></returns>
		internal VersionScenarioInfo VersionScenarioBuilderYM (VersionScenario scenario) {
			VersionScenarioInfo version = new VersionScenarioInfo();
			string branchAlpha = "alpha";
			string branchMain = "main";
			string branchBeta = "beta";
			string branchRC = "rc";

			string currentBranchName = "";
			PublishTargetEnum publishTarget = PublishTargetEnum.Production;
			DateTime currentDateTime = DateTime.Now;

			switch ( scenario ) {
				case VersionScenario.InitialAlpha:
					version.MainBranch = new SemVersion(0);
					version.CurrentBranch = new SemVersion(0);
					version.CurrentBranchName = branchAlpha;
					version.PublishTarget = PublishTargetEnum.Alpha;
					version.ExpectedVersion = new SemVersion(currentDateTime.Year, currentDateTime.Month, 0, "alpha.0000");
					break;
				
				case VersionScenario.InitialBeta:
					version.MainBranch = new SemVersion(0);
					version.CurrentBranch = new SemVersion(0);
					version.CurrentBranchName = branchBeta;
					version.PublishTarget = PublishTargetEnum.Beta;
					version.ExpectedBranchName = "beta";
					version.ExpectedVersion = new SemVersion(currentDateTime.Year, currentDateTime.Month, 0, "beta.0000");
					break;
				case VersionScenario.InitialProd:
					version.MainBranch = new SemVersion(0);
					version.CurrentBranch = new SemVersion(0);
					version.CurrentBranchName = branchMain;
					version.PublishTarget = PublishTargetEnum.Production;
					version.ExpectedBranchName = branchMain;
					version.ExpectedVersion = new SemVersion(currentDateTime.Year, currentDateTime.Month, 0);
					break;

				default: break;
			}

			return version;
		}
	}


	public enum VersionScenario {
		InitialAlpha = 0, 
		InitialBeta = 1,
		InitialProd = 2,
	}
}