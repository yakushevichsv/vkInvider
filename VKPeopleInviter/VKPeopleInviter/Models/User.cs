using System;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	[JsonObject]
	public class User
	{
		[JsonProperty ("uid")]
		public string Id { get; set; }

		[JsonProperty ("first_name")]
		public string FirstName { get; set; }

		[JsonProperty ("last_name")]
		public string LastName { get; set; }
     
		[JsonProperty ("photo_100")]
		public string Picture { get; set; }

		[JsonProperty ("sex")]
		public string Gender { get; set; }

		[JsonIgnore]
		public ImageSource PictureSource
		{
			get
			{
				return new UriImageSource() { Uri = new Uri(Picture)};
			}
		}
		[JsonIgnore]
		public string FullName
		{
			get
			{
				return FirstName + " " + LastName;
			}
		}

		[JsonIgnore]
		public string Token { get; set;}

		[JsonIgnore]
		public DateTime ExpirationDate { get; set;}
	}
}
