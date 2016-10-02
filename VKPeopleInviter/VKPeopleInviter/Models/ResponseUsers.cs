using Newtonsoft.Json;

namespace VKPeopleInviter
{
    [JsonObject]
    public class ResponseUsers
    {
        [JsonProperty("response")]
        public User[] users { get; set; }
    }
}
