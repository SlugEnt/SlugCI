using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Slug.CI.SlugBuildStages
{

	/// <summary>
	/// All of the build stages
	/// </summary>
	public static class BuildStageStatic {
		public const string STAGE_CALCVERSION = "Calculate Version #";
		public const string STAGE_COVER = "Code Coverage";
		public const string STAGE_PUBLISH = "Publish";
		public const string STAGE_PACK = "Pack";
		public const string STAGE_TEST = "Unit Tests";
		
		public const string STAGE_COMPILE = "Compile";
		public const string STAGE_RESTORE = "Restore";
		public const string STAGE_CLEAN = "Clean";
		public const string STAGE_GITCOMMIT = "Git Commit";
		public const string STAGE_GITCLEAN = "Git Clean";
		public const string STAGE_TYPEWRITER_VER = "TypeWriter Build";
		public const string STAGE_TYPEWRITER_PUBLISH = "TypeWriter";
		public const string STAGE_ANGULAR = "Angular Compile";
		public const string STAGE_FINAL = "Final";
	}
}
