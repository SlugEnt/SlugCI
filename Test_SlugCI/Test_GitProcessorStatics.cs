using NUnit.Framework;
using System;
using Semver;
using Slug.CI;

namespace Test_SlugCI
{
	[TestFixture]
	public class Test_GitProcessorStatic
	{
		/// <summary>
		/// Ensure we can correctly convert a Git version tag to a SemVersion object
		/// </summary>
		/// <param name="version"></param>
		/// <param name="shouldError"></param>
		/// <param name="major"></param>
		/// <param name="minor"></param>
		/// <param name="patch"></param>
		/// <param name="alpha"></param>
		[TestCase("Ver1.2.3",false,1,2,3,"")]
		[TestCase("Ver1.2.3-alpha233", false, 1, 2, 3, "alpha233")]
		[TestCase("Ver1.2.3-beta", false, 1, 2, 3, "beta")]
		[TestCase("Ver1.2.3-beta.6", false, 1, 2, 3, "beta.6")]
		[TestCase("ver1.2.3", true, 1, 2, 3, "")]
		[TestCase("Vera.2.3", true, 1, 2, 3, "")]
		[TestCase("Ver1.B.3", true, 1, 2, 3, "")]
		[TestCase("Ver1.2.C", true, 1, 2, 3, "")]
		[TestCase("Ver1.2.3:alpha", true, 1, 2, 3, "")]
		[TestCase("1.2.3", true, 1, 2, 3, "")]
		[TestCase("Ver1.2.alpha", true, 1, 2, 3, "")]
		[TestCase("Ver1", true, 1, 2, 3, "")]
		[TestCase("Ver1.2", true, 1, 2, 3, "")]
		[TestCase("Ver1-2-3", false, 1, 2, 3, "")]
		[Test]
		public void Test_ConvertVersionToSemVer(string version, bool shouldError, int major, int minor, int patch, string alpha) {
			if (!shouldError)
				Assert.DoesNotThrow(()=> GitProcessor.ConvertVersionToSemVersion(version),"A10:");
			else {
				Assert.Throws<Exception>(() => GitProcessor.ConvertVersionToSemVersion(version),"A20:");
				return;
			}


			// Now check the versioning
			SemVersion semVersion = GitProcessor.ConvertVersionToSemVersion(version);
			Assert.AreEqual(major,semVersion.Major,"A100");
			Assert.AreEqual(minor,semVersion.Minor,"A120");
			Assert.AreEqual(patch, semVersion.Patch,"A130");
			Assert.AreEqual(alpha,semVersion.Prerelease,"A140");
		}


		/// <summary>
		/// Validates that we properly handle Git Describe --Tags output and convert it correctly.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="throwsError"></param>
		/// <param name="tag"></param>
		/// <param name="commits"></param>
		/// <param name="hash"></param>
		[TestCase("Ver1.3.6-4-g4ftg45hs",false,"Ver1.3.6",4,"4ftg45hs")]
		[TestCase("Ver1.3.6-alpha.2323-4-g4ftg45hs", false, "Ver1.3.6-alpha.2323", 4, "4ftg45hs")]
		[TestCase("Ver1.3.6-alpha-23-4-g4ftg45hs", false, "Ver1.3.6-alpha-23", 4, "4ftg45hs")]
		[TestCase("Ver1.3.6-alpha-23-43-g4ftg45hs", false, "Ver1.3.6-alpha-23", 43, "4ftg45hs")]
		[TestCase("Ver1.3.6-4-g4ftg45hs1922", false, "Ver1.3.6", 4, "4ftg45hs1922")]
		[TestCase("Ver1.3.6-4-r4ftg45hs", true, "Ver1.3.6", 4, "4ftg45hs")]
		[TestCase("Ver1.3.6-s-g4ftg45hs", true, "Ver1.3.6", 4, "4ftg45hs")]
		[TestCase("Ver1.3.6", true, "Ver1.3.6", 4, "4ftg45hs")]
		[TestCase("Ver1.3.6-4", true, "Ver1.3.6", 4, "4ftg45hs")]
		[Test]
		public void GetGitDescribeTag_Success (string output, bool throwsError, string tag, int commits, string hash) {
			RecordGitDescribeTag record = new RecordGitDescribeTag("", 0, "");

			// Test
			if (! throwsError )
				Assert.DoesNotThrow(() => record = GitProcessor.GetGitDescribeTag(output),"A10:");
			else {
				Assert.Throws<Exception>(() => GitProcessor.GetGitDescribeTag(output), "A20:");
				return;
			}

			// Validate
			Assert.AreEqual(tag,record.tag,"A30:");
			Assert.AreEqual(commits,record.commitsSince,"A40:");
			Assert.AreEqual(hash,record.commitHash,"A50:");
		}
	}
}
