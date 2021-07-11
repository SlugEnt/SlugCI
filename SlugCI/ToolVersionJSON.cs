using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slug.CI
{
	/// <summary>
	/// Class is used to store information about the ToolVersion.  Tools can use this to check and see if they are current.
	/// </summary>
	public class ToolVersionJSON
	{
		public string ToolVersion { get; set; }
		public ToolVersionJSON () {}

		public static JsonSerializerOptions SerializerOptions()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonStringEnumConverter());
			options.WriteIndented = true;
			return options;
		}
	}


	
}
