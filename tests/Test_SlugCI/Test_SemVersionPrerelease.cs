using NUnit.Framework;
using System;
using Slug.CI;

namespace Test_SlugCI
{
	[TestFixture]
	public class Test_SemVersionPrerelease
	{
		[TestCase("alpha.1012", true,"alpha",1012,IncrementTypeEnum.None)]
		[TestCase("beta.1012", true, "beta", 1012, IncrementTypeEnum.None)]
		[TestCase("beta.1012", true, "beta", 1012, IncrementTypeEnum.None)]
		[TestCase("beta.1012a", true, "beta", 1012, IncrementTypeEnum.Patch)]
		[TestCase("beta.1012b", true, "beta", 1012, IncrementTypeEnum.Minor)]
		[TestCase("beta.1012c", true, "beta", 1012, IncrementTypeEnum.Major)]
		[TestCase("beta.0000", true, "beta", 0, IncrementTypeEnum.None)]
		[TestCase("gamma.0000", false, "beta", 0, IncrementTypeEnum.None)]
		[TestCase("beta.gamma", false, "beta", 0, IncrementTypeEnum.None)]
		[TestCase("beta.0001t", false, "beta", 0, IncrementTypeEnum.None)]
		[Test]
		public void Constructor_String__Success(string preTag, bool success,string releaseType,int numValue,IncrementTypeEnum incrementType) {
			if ( success ) {
				Assert.DoesNotThrow(() =>  new SemVersionPreRelease(preTag),"A10:");
				SemVersionPreRelease sem = new SemVersionPreRelease(preTag);
				Assert.AreEqual(releaseType,sem.ReleaseType,"A20:");
				Assert.AreEqual(numValue,sem.ReleaseNumber);
				Assert.AreEqual(incrementType, sem.IncrementType,"A30:");
			}
			else {
				Assert.Throws<Exception>(() => new SemVersionPreRelease(preTag), "A100:");
			}
			
		}

		[TestCase("alpha",4,IncrementTypeEnum.None,"alpha-0004")]
		[TestCase("alpha", 4, IncrementTypeEnum.Patch, "alpha-0004a")]
		[TestCase("alpha", 4, IncrementTypeEnum.Minor, "alpha-0004b")]
		[TestCase("alpha", 4, IncrementTypeEnum.Major, "alpha-0004c")]

		public void Tag_success (string releaseType, int numeric, IncrementTypeEnum incrementType,string expTag) {
			SemVersionPreRelease sem = new SemVersionPreRelease(releaseType, numeric, incrementType);
			Assert.AreEqual(releaseType,sem.ReleaseType,"A10:");
			Assert.AreEqual(numeric,sem.ReleaseNumber,"A20:");
			Assert.AreEqual(incrementType,sem.IncrementType,"A30:");
			Assert.AreEqual(expTag,sem.Tag(),"A40:");
		}


		[TestCase("alpha.0016c", IncrementTypeEnum.Major, "alpha-0017c")]
		[TestCase("alpha.0016c", IncrementTypeEnum.Minor, "alpha-0017c")]
		[TestCase("alpha.0016c", IncrementTypeEnum.Patch, "alpha-0017c")]
		[TestCase("alpha.0016c", IncrementTypeEnum.None, "alpha-0017c")]
		[TestCase("alpha.0016b", IncrementTypeEnum.Major, "alpha-0017c")]
		[TestCase("alpha.0013c", IncrementTypeEnum.Minor, "alpha-0014c")]
		[TestCase("alpha.0013b", IncrementTypeEnum.Minor, "alpha-0014b")]
		[TestCase("alpha.0013a", IncrementTypeEnum.Minor, "alpha-0014b")]
		[TestCase("alpha.0011c", IncrementTypeEnum.Patch, "alpha-0012c")]
		[TestCase("alpha.0011b", IncrementTypeEnum.Patch, "alpha-0012b")]
		[TestCase("alpha.0011a", IncrementTypeEnum.Patch, "alpha-0012a")]
		[TestCase("alpha.0011a", IncrementTypeEnum.None, "alpha-0012a")]
		[TestCase("alpha.0011", IncrementTypeEnum.None, "alpha-0012")]
		[TestCase("alpha.0011", IncrementTypeEnum.Major, "alpha-0012c")]
		[TestCase("alpha.0011", IncrementTypeEnum.Minor, "alpha-0012b")]
		[TestCase("alpha.0011",IncrementTypeEnum.Patch,"alpha-0012a")]
		[Test]
		public void BumpPatch_Success (string preRelease, IncrementTypeEnum incrementType, string expected) {
			SemVersionPreRelease start = new SemVersionPreRelease(preRelease);
			if (incrementType == IncrementTypeEnum.Patch) 
				start.BumpPatch();
			else if (incrementType == IncrementTypeEnum.Minor) 
				start.BumpMinor();
			else if (incrementType == IncrementTypeEnum.Major)
				start.BumpMajor();
			else 
				start.BumpVersion();

			SemVersionPreRelease end = new SemVersionPreRelease(expected);

			Assert.AreEqual(expected,start.Tag(),"A10:");
			Assert.AreEqual(end.ReleaseNumber, start.ReleaseNumber, "A20:");
			Assert.AreEqual(start.ReleaseType, end.ReleaseType, "A30:");
			Assert.AreEqual(end.IncrementType, start.IncrementType, "A40:");
		}
	}
}
