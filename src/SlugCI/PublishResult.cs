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
		public SlugCIDeployMethod DeployMethod { get; set; }

		public bool? CompileSuccess { get; set; } = null;
		public bool? PackedSuccess { get; set; } = null;
		public bool? PublishedSuccess { get; set; } = null;

		/// <summary>
		/// Whether the publishing was successful
		/// </summary>
		public bool WasSuccessful { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PublishResultRecord(SlugCIProject project)
		{
			NameOfProject = project.Name;
			DeployMethod = project.Deploy;
		}


		/// <summary>
		/// Angular Project Constructor
		/// </summary>
		/// <param name="project"></param>
		public PublishResultRecord (AngularProject project) {
			NameOfProject = project.Name;

			// All Angular projects are Copy
			DeployMethod = SlugCIDeployMethod.Copy;
		}
	}
}
