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

namespace Test_SlugCI
{
	[TestFixture]
	public class Test_StageCalcVersion
	{
		[Test]
		
		public void ProdToAlpha () {
			// A. Setup
			CISession ciSession = new();
			BuildStage_CalcVersion buildStageCalcVersion = new(ciSession);

			SemVersion currentMainVersion = new(1,0,0);
			SemVersion currentBranchVersion = new(0,1,1);
			PublishTargetEnum publishTarget = PublishTargetEnum.Alpha;
			string currentBranchName = "alpha";
			string mainBranchName = "main";
			bool useYMSchema = true;

			// Test
			SemVersion nextVersion = buildStageCalcVersion.CalculateNextVersion(currentMainVersion,currentBranchVersion,publishTarget,currentBranchName, mainBranchName,useYMSchema);
			
		}
	}
}
