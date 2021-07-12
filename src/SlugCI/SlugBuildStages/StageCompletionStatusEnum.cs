namespace Slug.CI
{
	/// <summary>
	/// The completion status of a stage, Failed, Success or Warning
	/// </summary>
	public enum StageCompletionStatusEnum
	{
		/// <summary>
		/// Stage has not been started.
		/// </summary>
		NotStarted = 0, 


		/// <summary>
		/// Stage was not run, either due to failed dependencies or pre-stage errors
		/// </summary>
		Aborted = 40,


		/// <summary>
		/// Stage was unable to successfully complete.
		/// </summary>
		Failure = 100,


		/// <summary>
		/// Stage was purposefully skipped
		/// </summary>
		Skipped = 150,


		/// <summary>
		/// Stage completed, but it might not have done so successfully.
		/// </summary>
		Warning = 200,


		/// <summary>
		/// Stage was started and is in process.
		/// </summary>
		InProcess = 220,


		/// <summary>
		/// Stage completed successfully
		/// </summary>
		Success = 254,
	}
}
