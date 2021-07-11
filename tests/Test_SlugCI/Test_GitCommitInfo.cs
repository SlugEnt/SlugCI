using NUnit.Framework;
using System;
using Slug.CI;

namespace Test_SlugCI
{
	[TestFixture]
	public class Test_GitCommitInfo
	{
		[Test]
		public void ConstructorFromStringBasics_Success () {
			string gitOutput = "dab4612|Bob Marley|1621171608|fax increased| (origin/beta)";

			GitCommitInfo git = new GitCommitInfo(gitOutput);

			Assert.AreEqual("dab4612",git.CommitHash,"A10:");
			Assert.AreEqual("Bob Marley",git.Committer,"A20:");
			Assert.AreEqual("fax increased", git.Message, "A30:");
			Assert.AreEqual(0,git.Tags.Count,"A40:");
			Assert.AreEqual(1,git.Branches.Count,"A50:");
			Assert.AreEqual("origin/beta",git.Branches[0],"A60:");
			DateTime d1 = new DateTime(2021, 5, 16, 9, 26, 48);
			Assert.AreEqual(d1,git.DateCommitted,"A70:");
		}


		[Test]
		public void MultipleGitTags () {
			string gitOutput = "51514b1|Personal|1621683273|Merging Branch: fix/somestuff   |  1.0.14| (tag: moreinfo, tag: Ver1.0.14, origin/main, main)";

			GitCommitInfo git = new GitCommitInfo(gitOutput);
			Assert.AreEqual(2,git.Tags.Count,"A10:");
			Assert.AreEqual("moreinfo",git.Tags[0],"A20:");
			Assert.AreEqual("Ver1.0.14", git.Tags[1], "A30:");
		}


		[Test]
		public void Branches_Success () {
			string gitOutput = "65a8e52|Personal|1621856829|Merge branch 'fix/PrinterJams' into alpha| (tag: Ver1.1.0-alpha.0006b, origin/alpha, alpha)";

			GitCommitInfo git = new GitCommitInfo(gitOutput);
			Assert.AreEqual(2, git.Branches.Count, "A10:");
			Assert.AreEqual("origin/alpha", git.Branches[0],"A20:");
			Assert.AreEqual("alpha",git.Branches[1],"A30:");
		}
	}
}
