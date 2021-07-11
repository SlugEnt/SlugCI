using Slug.CI;
using Slug.CI.NukeClasses;

namespace Test_SlugCI
{
	internal class testStageClass : BuildStage {
		internal testStageClass (string name, CISession ciSession) : base(name, ciSession) {
		}

		protected override StageCompletionStatusEnum ExecuteProcess() {
			int x = 0;
			return StageCompletionStatusEnum.Success;
		}
	}
}
