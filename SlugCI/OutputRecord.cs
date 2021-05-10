using Nuke.Common;
using System;
using System.Collections.Generic;
using System.Text;


namespace Slug.CI
{
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
