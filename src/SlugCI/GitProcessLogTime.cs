using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slug.CI
{
	/// <summary>
	/// Used to assist in debugging Git commands run time
	/// </summary>
	public class GitProcessLogTime
	{
		public long ElapsedMS { get; set; }
		public string Command { get; set; }
		public string Arguments { get; set; }


		public GitProcessLogTime (string command, string arguments, long elapsedMs) {
			ElapsedMS= elapsedMs;
			Command = command;
			Arguments = arguments;
		}


		public string Output () {
			return String.Format("[{0}] {1} :  {2}", ElapsedMS,Command,Arguments);
		}
	}
}
