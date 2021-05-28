using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Drawing;
using System.Text;
using Nuke.Common;

namespace Slug.CI
{
	/// <summary>
	///  The Sequence of stages that have been requested to run.
	/// </summary>
	public class ExecutionPlan
	{
		// The actual stages in order of planned execution
		private LinkedList<BuildStage> _executionPlan = new LinkedList<BuildStage>();

		// The list of all available stages
		private List<BuildStage> _buildStages = new List<BuildStage>();
		
		public StageCompletionStatusEnum PlanStatus { get; private set; }


		/// <summary>
		/// Provides a list of all the known stages.
		/// </summary>
		public List<BuildStage> KnownStages {
			get { return  _buildStages; }
		}


		/// <summary>
		/// Provides the execution plan
		/// </summary>
		public LinkedList<BuildStage> Plan {
			get { return _executionPlan; }
		}



		/// <summary>
		/// Constructor
		/// </summary>
		public ExecutionPlan () {
			PlanStatus = StageCompletionStatusEnum.NotStarted;
		}


		/// <summary>
		/// Adds the given Build Stage to the known List
		/// </summary>
		/// <param name="buildStage"></param>
		public void AddKnownStage (BuildStage buildStage) {
			_buildStages.Add(buildStage);
		}



		/// <summary>
		/// Gets the requested build stage from the Known Build Stages list.   Throws exception if build stage is not found.
		/// </summary>
		/// <param name="buildStageName">Name of the build stage to retrieve</param>
		/// <returns></returns>
		public BuildStage GetBuildStage(string buildStageName)
		{
			// Find the target in the build stages list
			BuildStage buildStage = _buildStages.Find(x => x.Name ==buildStageName);
			if (buildStage == null) throw new ApplicationException("The requested build stage of [" + buildStageName + "] is not a valid build stage.");
			return buildStage;
		}



		/// <summary>
		/// Builds the planned processing sequence for the build stages, depending on the final requested build stage
		/// </summary>
		/// <param name="endingStageName">The final build stage to stop at.</param>
		public void BuildExecutionPlan(string endingStageName) {
			BuildStage buildStage = GetBuildStage(endingStageName);
			AddBuildStage_ToExecutionPlan(buildStage, null);
		}



		/// <summary>
		/// Inserts the requested buildStage into the Execution plan.  It will then add all predecessor Build Stages as well.
		/// </summary>
		/// <param name="buildStage"></param>
		/// <param name="parent"></param>
		private void AddBuildStage_ToExecutionPlan(BuildStage buildStage, BuildStage parent)
		{
			LinkedListNode<BuildStage> newNode = new LinkedListNode<BuildStage>(buildStage);


			// Add ourselves into the Execution plan at the proper place.
			if (parent == null)
				_executionPlan.AddFirst(newNode);
			else
			{
				// Need to ensure we add this before the parent
				LinkedListNode<BuildStage> parentNode = _executionPlan.Find(parent);
				if (parentNode == null) throw new ApplicationException("Failed to locate the parent node for the BuildStage object [" + buildStage.Name + "]");

				// Determine if we are already in the list (Maybe some other stage also has us as a predecessor..)
				LinkedListNode<BuildStage> myself = _executionPlan.Find(buildStage);
				if ( myself != null ) {
					// Need to make sure the parent is not before us...  If so we need to move ourselves, to before the parent
					bool parentExistsPrior = false;
					bool continueSearching = true;
					LinkedListNode<BuildStage> priorParentLinkedListNode = myself.Previous;
					while ( continueSearching ) {
						// We were already at first item there are no more. 
						if ( priorParentLinkedListNode == null ) continueSearching = false;
						
						// We found parent before us.  We need to move ourselves to before the parent.
						else if ( priorParentLinkedListNode.Value == parent ) {
							_executionPlan.Remove(myself);
							_executionPlan.AddBefore(priorParentLinkedListNode, myself);
							continueSearching = false;
						}
						else if ( priorParentLinkedListNode == _executionPlan.First )
							continueSearching = false;
						else
							priorParentLinkedListNode = priorParentLinkedListNode.Previous;
					}
				}
				else {
					myself = newNode;

					// Add ourselves ahead of the parent
					_executionPlan.AddBefore(parentNode, myself);
				}
			}

			// Now, add our predecessors
			foreach (string predecessor in buildStage.PredecessorList)
			{
				BuildStage childBuildStage = GetBuildStage(predecessor);
				AddBuildStage_ToExecutionPlan(childBuildStage, buildStage);
			}

		}


		
		/// <summary>
		/// Executes the build plan
		/// </summary>
		/// <returns></returns>
		public StageCompletionStatusEnum Execute () {
			PlanStatus = StageCompletionStatusEnum.InProcess;
			StageCompletionStatusEnum stageStatus;

			try {
				foreach ( BuildStage buildStage in Plan ) {
					buildStage.Execute();
					stageStatus = buildStage.CompletionStatus;
//					if ( FinalStatus > StageCompletionStatusEnum.NotStarted && stageStatus < FinalStatus) { FinalStatus = stageStatus; }

					// Anything less than Skipped is an error and we stop immediately.
					if ( stageStatus < StageCompletionStatusEnum.Skipped ) { break; }
				}


				// We have processed everything...Now loop thru all the stages to determine final status
				PlanStatus = StageCompletionStatusEnum.Success;
				foreach ( BuildStage buildStage in Plan ) {
					if ( buildStage.CompletionStatus < PlanStatus ) PlanStatus = buildStage.CompletionStatus;
				}
			}

			catch ( Exception e ) {
				PlanStatus = StageCompletionStatusEnum.Failure;
				Logger.Error(e);
			}

			return PlanStatus;
		}



		/// <summary>
		/// Returns true if the Plan was successfully executed.
		/// </summary>
		public bool WasSuccessful {
			get {
				if ( PlanStatus >= StageCompletionStatusEnum.Skipped ) return true;
				return false;
			}
		}

	}
}
