using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Microsoft.Build.Tasks;
using Console = Colorful.Console;


namespace SlugCI
{
	public static class Misc
	{
		public static void WriteHeader(string text)
		{
			Console.WriteLineWithGradient("    |-------------------------------------------------|",Color.Yellow, Color.DarkOrange);
			Console.WriteLine("    |   " + text);
			Console.WriteLineWithGradient("    |-------------------------------------------------|", Color.DarkOrange, Color.DarkViolet);
		}
	}
}
