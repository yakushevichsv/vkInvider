using System;

namespace VKPeopleInviter
{
	public static class Constants
	{
		public static string CitiesKey = "CitiesKey";
		public static string InvitationTemplateKey = "InvitationTemplateKey";

		// OAuth
		public static long ClientId = 5537512;
		public static string ClientSecret = "E9x6ywxHcYnnqf3ZXtjd";

		public static long Group1ToUseId = 72566211;
		public static long CityCodeId = 5835;

		public static long GroupId = 100802490;

		// These values do not need changing
		public static string Scope = "friends,video,groups,offline,messages";
		public static string AuthorizeUrl = "https://oauth.vk.com/authorize";
		//public static string AccessTokenUrl = "https://accounts.google.com/o/oauth2/token";
		public static string UserInfoUrl = "https://api.vk.com/method/users.get";

		// Set this property to the location the user will be redirected too after successfully authenticating
		public static string RedirectUrl = "https://oauth.vk.com/blank.html";

		public static string[] ExternalGroupdIds
		{
			get
			{
				return new string[] { Group1ToUseId.ToString() };
			}
		}
	}
}
