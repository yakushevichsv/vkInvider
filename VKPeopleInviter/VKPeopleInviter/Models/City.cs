using System;
using Newtonsoft.Json;

namespace VKPeopleInviter
{
	[JsonObject]
	public class City
	{
		[JsonProperty("title")]
		public string Name { get; set;}

		[JsonProperty("region")]
		public string Region { get; set;}

		[JsonProperty("area")]
		public string Area { get; set; }

		[JsonProperty("Id")]
		public string Id { get; set; }

		public City(string name, string id): this(name, id, string.Empty, string.Empty) {}

		public City(string name, string id, string region, string area)
		{
			this.Id = id;
			this.Name = name;
			this.Region = region;
			this.Area = area;
		}
	}
}
