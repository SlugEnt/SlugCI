using System;
using System.Collections.Generic;
using System.Text;

namespace Slug.CI
{
	/// <summary>
	/// The completion status of a stage, Failed, Success or Warning
	/// </summary>
	public enum StageCompletionStatusEnum
	{
		/// <summary>
		/// Stage was not run, either due to failed dependencies or pre-stage errors
		/// </summary>
		Aborted = 0,

		/// <summary>
		/// Stage was unable to successfully complete.
		/// </summary>
		Failure = 1,


		/// <summary>
		/// Stage was puposefully skipped
		/// </summary>
		Skipped = 10,

		/// <summary>
		/// Stage completed, but it might not have done so successfully.
		/// </summary>
		Warning = 100,

		/// <summary>
		/// Stage completed successfully
		/// </summary>
		Success = 254,
	}
}
