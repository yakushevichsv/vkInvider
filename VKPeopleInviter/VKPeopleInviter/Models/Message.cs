using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VKPeopleInviter
{
	[JsonObject]
	public class Message
	{
		[JsonProperty("mid")]
		public long MessageId { get; set;}

		[JsonProperty("uid")]
		public long UserId { get; set; }

		[JsonProperty("date")]
		[JsonConverter(typeof(UnixDateTimeConverter))]
		public DateTime UnixTime { get; set; }

		[JsonProperty("out")]
		public bool IsOut { get; set; }

		[JsonProperty("read_state", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsRead { get; set; }

		[JsonProperty("deleted", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsDeleted { get; set; }

		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("body", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Body { get; set; }
	}

	public class UnixDateTimeConverter : DateTimeConverterBase
	{
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.Integer)
			{
				throw new Exception(
					String.Format("Unexpected token parsing date. Expected Integer, got {0}.",
					reader.TokenType));
			}

			var ticks = (long)reader.Value;

			var date = new DateTime(1970, 1, 1);
			date = date.AddSeconds(ticks);
			return date;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			long ticks;
			if (value is DateTime)
			{
				var epoc = new DateTime(1970, 1, 1);
				var delta = ((DateTime)value) - epoc;
				if (delta.TotalSeconds < 0)
					throw new ArgumentException("Unix epoc starts January 1st, 1970"); 
				ticks = (long)delta.TotalSeconds;
			}
			else
				throw new Exception("Expected date object value.");
			
			writer.WriteValue(ticks);
		}
	}
}
