using System;
using System.Collections.Generic;
using System.Text;

namespace Slug.CI
{

	/// <summary>
	/// Used for storing the result of a single Publish Operation.
	/// </summary>
	public class PublishResultRecord
	{
		/// <summary>
		/// Project Name
		/// </summary>
		public string NameOfProject { get; set; }

		/// <summary>
		/// How deployed
		/// </summary>
		public string DeployMethod { get; set; }

		/// <summary>
		/// Name of the Deploy
		/// </summary>
		public string DeployName { get; set; }

		/// <summary>
		/// Whether the publishing was successful
		/// </summary>
		public bool WasSuccessful { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nameOfProject">Project Name</param>
		/// <param name="deployMethod">How it was deployed</param>
		/// <param name="deployTarget">Name of the deploy target</param>
		/// <param name="wasSuccessful">True if it completed successfully</param>
		public PublishResultRecord(string nameOfProject, string deployMethod, string deployTarget, bool wasSuccessful = true)
		{
			NameOfProject = nameOfProject;
			DeployMethod = deployMethod;
			DeployName = deployTarget;
			WasSuccessful = wasSuccessful;
		}
	}
}
