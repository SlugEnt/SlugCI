using System.Drawing;
using Console = Colorful.Console;

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
		/// Default background color.
		/// </summary>
		public static Color DefaultBackgroundColor { get; set; } = Color.WhiteSmoke;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		public LineOut (string text, Color textColor) {
			Text = text;
			FGColor = textColor;
			BGColor = DefaultBackgroundColor;
		}


		/// <summary>
		/// Writes an Error line
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Error (string text) {
			LineOut lo = new LineOut(text,Color.Red);
			return lo;
		}


		/// <summary>
		/// Writes a line of Success text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Success(string text)
		{
			LineOut lo = new LineOut(text, Color.Green);
			return lo;
		}



		/// <summary>
		/// Writes a line of Warning text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Warning(string text)
		{
			LineOut lo = new LineOut(text, Color.Yellow);
			return lo;
		}


		/// <summary>
		/// Writes a line of Informational text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Info(string text)
		{
			LineOut lo = new LineOut(text, Color.Cyan);
			return lo;
		}


		/// <summary>
		/// Writes a line of normal text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static LineOut Normal(string text)
		{
			LineOut lo = new LineOut(text, Color.WhiteSmoke);
			return lo;
		}


		/// <summary>
		///  New Line!
		/// </summary>
		/// <returns></returns>
		public static LineOut NewLine () {
			LineOut lo = new LineOut("",Color.WhiteSmoke);
			return lo;
		}


		/// <summary>
		/// Writes the record to the Console.
		/// </summary>
		public void WriteToConsole () {
			Console.WriteLine(Text,FGColor);
		}
	}
}
