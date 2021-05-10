using System;
using System.Collections.Generic;
using System.Text;
using Slug.CI;
using Slug.CI.NukeClasses;

namespace Test_SlugCI
{
	internal class testStageClass : BuildStage {
		internal testStageClass (string name, CISession ciSession) : base(name, ciSession) {
		}



	}
}
