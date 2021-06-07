using Nuke.Common;

namespace Slug.CI
{

	/// <summary>
	/// Represents a printed information line for Severe messages during summary reporting.
	/// </summary>
	public class OutputRecord
	{
		public LogLevel LogLevel { get; private set; }
		public string Text { get; private set; }
		public string Details { get; private set; }


		public OutputRecord (LogLevel logLevel, string text, string details = null) {
			LogLevel = logLevel;
			Text = text;
			Details = details;
		}
	}
}
