using System.Drawing;
using Nuke.Common.Tooling;
using Console = Colorful.Console;
/*
namespace Slug.CI
{
	/// <summary>
	/// Used to store text that might need to be displayed to console eventually.  Allows the setting of colors to reproduce a colorful display just as if you were writing to console in real time.
	/// </summary>
	public class LineOut {
		/// <summary>
		///  Text Color
		/// </summary>
		public Color FGColor { get; private set; }

		/// <summary>
		/// Background color
		/// </summary>
		public Color BGColor { get; private set; }

		/// <summary>
		/// The text to store or display
		/// </summary>
		public string Text { get; private set; }


		/// <summary>
		/// Type of output line
		/// </summary>
		public OutputType OutputType { get; private set; }


		/// <summary>
		/// Default background color.
		/// </summary>
		public static Color DefaultBackgroundColor { get; set; } = Color.WhiteSmoke;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		public LineOut (OutputType type,string text, Color textColor) {
			Text = text;
			OutputType = type;
			FGColor = textColor;
			BGColor = DefaultBackgroundColor;
		}


		/// <summary>
		/// Writes an Error line
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Error (string text) {
			LineOut lo = new LineOut(OutputType.Err,text,Color.Red);
			return lo;
		}


		/// <summary>
		/// Writes a line of Success text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Success(string text)
		{
			LineOut lo = new LineOut(OutputType.Success,text, Color.Green);
			return lo;
		}



		/// <summary>
		/// Writes a line of Warning text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Warning(string text)
		{
			LineOut lo = new LineOut(OutputType.Warn,text, Color.Yellow);
			return lo;
		}


		/// <summary>
		/// Writes a line of Informational text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Info(string text)
		{
			LineOut lo = new LineOut(OutputType.Info,text, Color.Cyan);
			return lo;
		}


		/// <summary>
		/// Writes a line of normal text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Normal(string text)
		{
			LineOut lo = new LineOut(OutputType.Std,text, Color.WhiteSmoke);
			return lo;
		}
		

		/// <summary>
		/// Writes a line of normal text in the given color
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		/// <returns></returns>
		public static LineOut Normal(string text, Color textColor)
		{
			LineOut lo = new LineOut(OutputType.Std, text, textColor);
			return lo;
		}



		/// <summary>
		///  New Line!
		/// </summary>
		/// <returns></returns>
		public static LineOut NewLine () {
			LineOut lo = new LineOut(OutputType.Std,"",Color.WhiteSmoke);
			return lo;
		}


		/// <summary>
		/// Writes the record to the Console in color if raw is false
		/// <param name="raw"></param> If true, it writes to the console, just the text with a prefix marker stating what type of Output Type it is.
		/// </summary>
		public void WriteToConsole (bool raw = false) {
			if (raw)
				Console.WriteLine(ToString(),FGColor);
			else {
				Console.WriteLine(Text, FGColor);
			}
		}


		/// <summary>
		/// Just displays the text...
		/// </summary>
		/// <returns></returns>
		public override string ToString () {
			string prefix = "Norm --> ";
			if (OutputType == OutputType.Std)            prefix = "Std  --> ";
			else if (OutputType == OutputType.Info)      prefix = "Info --> ";
			else if ( OutputType == OutputType.Success ) prefix = "Ok   --> ";
			else if ( OutputType == OutputType.Warn )    prefix = "Warn --> ";
			else if ( OutputType == OutputType.Err )
				prefix = "Err  --> ";
			else
				prefix = "???  --> ";

			return prefix + Text;
		}
	}
}
*/
