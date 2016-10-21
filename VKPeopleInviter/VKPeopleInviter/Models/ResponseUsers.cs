using Newtonsoft.Json;

namespace VKPeopleInviter
{
    [JsonObject]
    public class ResponseItems<T>
    {
        [JsonProperty("response")]
        public T[] Items { get; set; }
    }

	[JsonObject]
	public class ListOfItems<T>
	{	
		[JsonProperty("count")]
		public long Count { get; set;}

		[JsonProperty("items")]
		public T[] Items { get; set; }
	}

	[JsonObject]
	public class ListOfItemsWrapper<T>
	{
		[JsonProperty("response")]
		public ListOfItems<T> List { get; set; }
	}

	public sealed class ResponseUsers : ResponseItems<User> { } 
	public sealed class TotalListOfUsers : ListOfItems<User> { }
	public sealed class TotalListOfUsersWrapper : ListOfItemsWrapper<User> {}

	public sealed class ResponseMessages : ResponseItems<Message> { }
	public class TotalListOfMessages : ListOfItems<Message> {} 
	public class TotalListOfMessagesWrapper : ListOfItemsWrapper<Message> {}
}
