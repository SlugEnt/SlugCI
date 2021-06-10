﻿using System.Collections.Generic;
using Nuke.Common.IO;

namespace Slug.CI
{
	/// <summary>
	/// Represents a Visual Studio Project for the Setup Target Functionality
	/// </summary>
	public class VisualStudioProject {
		public string Name { get; set; }
		public string Namecsproj { get; set; }
		public AbsolutePath OriginalPath { get; set; }
		public AbsolutePath NewPath { get; set; }
		public bool IsTestProject { get; set; }
		public List<string> Frameworks { get; set; } = new List<string>();
	}
}
