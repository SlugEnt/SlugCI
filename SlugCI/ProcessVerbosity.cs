using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slug.CI
{
	/// <summary>
	/// The verbosity setting that Command Process tasks can have:
	/// <para>All - Logs commands and their output</para>
	/// <para>Commands - Logs the commands, but not their output</para>
	/// <para>Output, just the output.</para>
	/// <para>In all cases errors are logged.</para>
	/// </summary>
	public enum ProcessVerbosity
	{

		/// <summary>
		/// Logs commands and their output
		/// </summary>
		Nothing = 0,

		/// <summary>
		/// Logs just the commands being run
		/// </summary>
		Commands = 10,

		/// <summary>
		/// Logs just the output of commands
		/// </summary>
		All = 20,

	}
}
