using Newtonsoft.Json;

namespace VKPeopleInviter
{
	[JsonObject]
	public class ResponseCities
	{
		[JsonProperty("response")]
		public CitiesDictionary Response { get; set; }
	}

	[JsonDictionary]
	public class CitiesDictionary
	{
		[JsonProperty("items")]
		public City[] Items { get; set; }
	}
}
