using Newtonsoft.Json;

namespace VKPeopleInviter
{
    [JsonObject]
    public class ResponseUsers
    {
        [JsonProperty("response")]
        public User[] users { get; set; }
    }

	[JsonObject]
	public class TotalListOfUsers
	{
		[JsonProperty("count")]
		public long Count { get; set; }

		[JsonProperty("items")]
		public User[] Users { get; set;}
	}

	[JsonObject]
	public class TotalListOfUsersWrapper
	{
		[JsonProperty("response")]
		public TotalListOfUsers totalListOfUsers { get; set;}
	}


}
