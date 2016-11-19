using System;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace VKPeopleInviter
{
	[JsonObject]
	public class User
	{
		public static string[] Pictures
		{
			get
			{
				return new string[] { "photo_100", "photo_200", "photo_400", "photo_50", "photo_max", "photo_max_orig" };
			}
		}

		public static string PicturesJoint
		{
			get
			{
				return Pictures.CommaJointItems();
			}
		}

		[JsonProperty("uid")]
		public long Id { get; set; }

		[JsonProperty("first_name")]
		public string FirstName { get; set; }

		[JsonProperty("last_name")]
		public string LastName { get; set; }

		[JsonProperty("photo_100", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture100 { get; set; }

		[JsonProperty("photo_200", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture200 { get; set; }

		[JsonProperty("photo_400", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture400 { get; set; }

		[JsonProperty("photo_50", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture50 { get; set; }

		[JsonProperty("photo_max", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string PictureMax { get; set; }

		[JsonProperty("photo_max_orig", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string PictureMaxOrigin { get; set; }

		[JsonProperty("sex", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Gender { get; set; }

		[JsonProperty("can_write_private_message", DefaultValueHandling = DefaultValueHandling.Ignore )]
		public bool CanWritePrivateMessage { get; set; }

		[JsonIgnore]
		public string ImageUri
		{
			get
			{
				string result = null;

				if (!string.IsNullOrEmpty(Picture200))
					result = Picture200;
				else if (!string.IsNullOrEmpty(Picture100))
					result = Picture100;
				else if (!string.IsNullOrEmpty(Picture400))
					result = Picture400;
				else if (!string.IsNullOrEmpty(PictureMax))
					result = PictureMax;
				else if (!string.IsNullOrEmpty(PictureMaxOrigin))
					result = PictureMaxOrigin;
				else if (!string.IsNullOrEmpty(Picture50))
					result = Picture50;


				return !string.IsNullOrEmpty(result) ? result : null;
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
		public string Token { get; set; }

		[JsonIgnore]
		public DateTime ExpirationDate { get; set; }

		[JsonProperty("city",DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long CityId;

		[JsonProperty("country", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long CountryId;

		[JsonProperty("home_town", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string HomeTown;
	}
}

