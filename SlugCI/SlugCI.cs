using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using System.Xml.XPath;
using Console = Colorful.Console;


namespace Slug.CI
{
	/// <summary>
	/// Sets up a solution to be compatible with SlugCI, including moving directories, ensuring config files, exist, etc.
	/// </summary>
	public class SlugCI
	{
		/// <summary>
		/// The solution projects Main folder.
		/// </summary>
		public AbsolutePath RootDirectory { get; set; }

		/// <summary>
		/// The RootCI folder
		/// </summary>
		public AbsolutePath SlugCIPath { get; set; }



		public SlugCI (string rootDir) {
			RootDirectory = (AbsolutePath) rootDir;
			SlugCIPath = RootDirectory / ".slugci";

			// Ensure Solution is in SlugCI format. If not migrate it.
			ConvertToSlugCI converter = new ConvertToSlugCI(RootDirectory);
		}
	}
}
