namespace Slug.CI.SlugBuildStages
{
	using System;
	using System.Collections.Generic;

	using System.Globalization;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public partial class TypeWriterConfig
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		/// <summary>
		/// The full semantic version include pre-release.  NPM does not use this.
		/// </summary>
		[JsonProperty("versionFull")]
		public string VersionFull { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("scripts")]
		public Scripts Scripts { get; set; }

		[JsonProperty("keywords")]
		public List<object> Keywords { get; set; }

		[JsonProperty("author")]
		public string Author { get; set; }

		[JsonProperty("license")]
		public string License { get; set; }

		[JsonProperty("devDependencies")]
		public DevDependencies DevDependencies { get; set; }

		[JsonProperty("publishConfig")]
		public PublishConfig PublishConfig { get; set; }
	}

	public partial class DevDependencies
	{
		[JsonProperty("typescript")]
		public string Typescript { get; set; }
	}

	public partial class PublishConfig
	{
		[JsonProperty("registry")]
		public Uri Registry { get; set; }
	}

	public partial class Scripts
	{
		[JsonProperty("copy")]
		public string Copy { get; set; }

		[JsonProperty("pack_publish")]
		public string PackPublish { get; set; }

		[JsonProperty("pack")]
		public string Pack { get; set; }

		[JsonProperty("publish")]
		public string Publish { get; set; }

	}
}
