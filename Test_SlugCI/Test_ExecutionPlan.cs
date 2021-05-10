using System.Collections.Generic;
using NUnit.Framework;
using Slug.CI;
using Slug.CI.NukeClasses;

namespace Test_SlugCI
{
	public class Test_ExecutionPlan
	{
		CISession ciSession = new CISession();


		[SetUp]
		public void Setup()
		{ }


		[Test]
		public void ExecutionPlan_2Stages_Success()
		{
			// A. Setup
			ExecutionPlan executionPlan = new ExecutionPlan();
			CISession ciSession = new CISession();

			const string stage_01 = "st01";
			const string stage_02 = "st02";
			const string stage_03 = "st03";
			const string stage_04 = "st04";


			// B. Build some Build Stages to test with
			testStageClass stage01 = new testStageClass(stage_01, ciSession);
			testStageClass stage02 = new testStageClass(stage_02, ciSession);

			// Setup predecessors
			stage02.AddPredecessor(stage_01);

			// B3 - Add to Execution plan
			executionPlan.AddKnownStage(stage01);
			executionPlan.AddKnownStage(stage02);

			// Test
			//Should be 2 steps in the plan and known stages
			executionPlan.BuildExecutionPlan(stage_02);

			// Validate
			Assert.AreEqual(2,executionPlan.KnownStages.Count,"A10: ");
			Assert.AreEqual(2,executionPlan.Plan.Count,"A20: ");

			// Validate the plan sequence
			Assert.AreEqual(stage01,executionPlan.Plan.First.Value,"A30:");
			Assert.AreEqual(stage02,executionPlan.Plan.First.Next.Value,"A40:");
		}



		[Test]
		public void ExecutionPlan_MultiDependency_Success()
		{
			// A. Setup
			ExecutionPlan executionPlan = new ExecutionPlan();
			CISession ciSession = new CISession();

			const string stage_01 = "st01";
			const string stage_02 = "st02";
			const string stage_03 = "st03";
			const string stage_04 = "st04";


			// B. Build some Build Stages to test with
			testStageClass stage01 = new testStageClass(stage_01, ciSession);
			testStageClass stage02 = new testStageClass(stage_02, ciSession);
			testStageClass stage03 = new testStageClass(stage_03, ciSession);
			testStageClass stage04 = new testStageClass(stage_04, ciSession);

			// Setup predecessors
			stage02.AddPredecessor(stage_01);
			stage03.AddPredecessor(stage_02);
			stage04.AddPredecessor(stage_02);
			stage04.AddPredecessor(stage_03);


			// B3 - Add to Execution plan
			executionPlan.AddKnownStage(stage01);
			executionPlan.AddKnownStage(stage02);
			executionPlan.AddKnownStage(stage03);
			executionPlan.AddKnownStage(stage04);


			// Test
			//Should be 2 steps in the plan and known stages
			executionPlan.BuildExecutionPlan(stage_04);

			// Validate
			Assert.AreEqual(4, executionPlan.KnownStages.Count, "A10: ");
			Assert.AreEqual(4, executionPlan.Plan.Count, "A20: ");

			// Validate the plan sequence
			LinkedListNode<BuildStage> node = executionPlan.Plan.First;
			Assert.AreEqual(stage01, node.Value, "A30:");
			node = node.Next;
			Assert.AreEqual(stage02, node.Value, "A40:");
			node = node.Next;
			Assert.AreEqual(stage03, node.Value, "A50:");
			node = node.Next;
			Assert.AreEqual(stage04, node.Value, "A60:");
		}



		[Test]
		public void ExecutionPlan_FullTest_Success()
		{
			// A. Setup
			ExecutionPlan executionPlan = new ExecutionPlan();
			CISession ciSession = new CISession();

			const string stage_01 = "st01";
			const string stage_02 = "st02"; 
			const string stage_03 = "st03"; 
			const string stage_04 = "st04"; 
			const string stage_05 = "st05"; 
			const string stage_06 = "st06"; 
			const string stage_07 = "st07"; 
			const string stage_08 = "st08";


			// B. Build some Build Stages to test with
			testStageClass stage01 = new testStageClass(stage_01,ciSession);
			testStageClass stage02 = new testStageClass(stage_02,ciSession);
			testStageClass stage03 = new testStageClass(stage_03, ciSession);
			testStageClass stage04 = new testStageClass(stage_04, ciSession);
			testStageClass stage05 = new testStageClass(stage_05, ciSession);
			testStageClass stage06 = new testStageClass(stage_06, ciSession);
			testStageClass stage07 = new testStageClass(stage_07, ciSession);
			testStageClass stage08 = new testStageClass(stage_08, ciSession);

			// Setup predecessors
			stage02.AddPredecessor(stage_01);
			stage03.AddPredecessor(stage_02);
			stage04.AddPredecessor(stage_02);
			stage04.AddPredecessor(stage_03);
			stage05.AddPredecessor(stage_04);
			stage07.AddPredecessor(stage_06);
			stage08.AddPredecessor(stage_07);
			stage08.AddPredecessor(stage_05);

			// B3 - Add to Execution plan
			executionPlan.AddKnownStage(stage01);
			executionPlan.AddKnownStage(stage02);
			executionPlan.AddKnownStage(stage03);
			executionPlan.AddKnownStage(stage04);
			executionPlan.AddKnownStage(stage05);
			executionPlan.AddKnownStage(stage06);
			executionPlan.AddKnownStage(stage07);
			executionPlan.AddKnownStage(stage08);


			// Test
			executionPlan.BuildExecutionPlan(stage_08);


			// Validate
			Assert.AreEqual(8, executionPlan.KnownStages.Count, "A10: ");
			Assert.AreEqual(8, executionPlan.Plan.Count, "A20: ");

			// Validate the plan sequence
			LinkedListNode<BuildStage> node = executionPlan.Plan.First;
			Assert.AreEqual(stage06, node.Value, "A30:");
			node = node.Next;
			Assert.AreEqual(stage07, node.Value, "A40:");
			node = node.Next;
			Assert.AreEqual(stage01, node.Value, "A50:");
			node = node.Next;
			Assert.AreEqual(stage02, node.Value, "A60:");
			node = node.Next;
			Assert.AreEqual(stage03, node.Value, "A80:");
			node = node.Next;
			Assert.AreEqual(stage04, node.Value, "A90:");
			node = node.Next;
			Assert.AreEqual(stage05, node.Value, "A60:");
			node = node.Next;
			Assert.AreEqual(stage08, node.Value, "A100:");




		}
	}
}