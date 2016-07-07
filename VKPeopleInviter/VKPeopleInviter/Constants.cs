using System;

namespace VKPeopleInviter
{
	public static class Constants
	{
		// AWS
		// Sign up for an AWS account at https://aws.amazon.com/
		// Configure at https://console.aws.amazon.com/cognito/
		public static string CognitoIdentityPoolId = "";

		// OAuth
		// For Google login, configure at https://console.developers.google.com/
		public static string ClientId = "5537512";
		public static string ClientSecret = "E9x6ywxHcYnnqf3ZXtjd";

		// These values do not need changing
		public static string Scope = "friends,video,groups";
		public static string AuthorizeUrl = "https://oauth.vk.com/authorize";
		//public static string AccessTokenUrl = "https://accounts.google.com/o/oauth2/token";
		public static string UserInfoUrl = "https://api.vk.com/method/users.get";

		// Set this property to the location the user will be redirected too after successfully authenticating
		public static string RedirectUrl = "https://oauth.vk.com/blank.html";
	}
}
