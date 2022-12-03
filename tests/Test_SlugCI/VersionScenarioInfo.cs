using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semver;
using Slug.CI;

namespace Test_SlugCI
{
	internal class VersionScenarioInfo {
		public SemVersion MainBranch;
		public SemVersion CurrentBranch;
		public string MainBranchName = "main";
		public string CurrentBranchName = "alpha";
		public PublishTargetEnum PublishTarget = PublishTargetEnum.Production;
		public SemVersion ExpectedVersion;
		public string ExpectedBranchName = "alpha";


	}
}
