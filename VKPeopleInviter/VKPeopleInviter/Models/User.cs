using System;
using Newtonsoft.Json;
using Xamarin.Forms;

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
				var result = string.Empty;

				foreach (var item in Pictures)
				{
					if (!string.IsNullOrEmpty(result))
						result += ",";

					result += item;
				}
				return result;
			}
		}

		[JsonProperty ("uid")]
		public string Id { get; set; }

		[JsonProperty ("first_name")]
		public string FirstName { get; set; }

		[JsonProperty ("last_name")]
		public string LastName { get; set; }
     
		[JsonProperty ("photo_100", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture100 { get; set; }

		[JsonProperty("photo_200", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture200 { get; set; }

		[JsonProperty("photo_400", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture400 { get; set;}

		[JsonProperty("photo_50", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Picture50 { get; set;}

		[JsonProperty("photo_max", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string PictureMax { get; set;}

		[JsonProperty("photo_max_orig", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string PictureMaxOrigin { get; set; }

		[JsonProperty ("sex", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Gender { get; set; }

		[JsonProperty("can_write_private_message")]
		public bool CanWritePrivateMessage { get; set;}

		[JsonIgnore]
		public ImageSource PictureSource
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
					 

				return !string.IsNullOrEmpty(result)  ? new UriImageSource() { Uri = new Uri(result)} : null;
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

public static class ArrayOfStringsExtension
{
	public static string FirstNonNullAndNonEmptyString(this string[] items)
	{
		if (items == null || items.Length == 0)
			return null;

		foreach (var item in items)
			if (!string.IsNullOrEmpty(item))
				return item;

		return null;
	}
}
