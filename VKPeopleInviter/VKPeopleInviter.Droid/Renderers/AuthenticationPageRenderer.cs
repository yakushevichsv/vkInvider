using Android.App;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using VKPeopleInviter;
using VKPeopleInviter.Droid;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer (typeof(AuthenticationPage), typeof(AuthenticationPageRenderer))]

namespace VKPeopleInviter.Droid
{
	// Use a custom page renderer to display the authentication UI on the AuthenticationPage
	public class AuthenticationPageRenderer : PageRenderer
	{
		bool isShown;

		protected override void OnElementChanged (ElementChangedEventArgs<Page> e)
		{
			base.OnElementChanged (e);

			// Retrieve any stored account information
			var accounts = AccountStore.Create (Context).FindAccountsForService (App.AppName);
			var account = accounts.FirstOrDefault ();

			if (account == null) {
				if (!isShown) {
					isShown = true;

					// Initialize the object that communicates with the OAuth service
					var auth = new OAuth2Authenticator (
						           Constants.ClientId,
						           Constants.Scope,
						           new Uri (Constants.AuthorizeUrl),
						           new Uri (Constants.RedirectUrl));

					// Register an event handler for when the authentication process completes
					auth.Completed += OnAuthenticationCompleted;

					// Display the UI
					var activity = Context as Activity;
					activity.StartActivity (auth.GetUI (activity));
				}
			} else {
				if (!isShown) {
                    string[] results = account.Username.Split(new string[]{" "},StringSplitOptions.RemoveEmptyEntries);
                    App.User.FirstName = results.First();
                    App.User.LastName = results.Last();
					App.SuccessfulLoginAction.Invoke ();
				}
			}
		}

		async void OnAuthenticationCompleted (object sender, AuthenticatorCompletedEventArgs e)
		{
			if (e.IsAuthenticated) {
                // If the user is authenticated, request their basic user data from Google
                // UserInfoUrl = https://www.googleapis.com/oauth2/v2/userinfo
                //Console.WriteLine(e.Account.ToString());
                string token = e.Account.Properties["access_token"].ToString() ;
                string userId = e.Account.Properties["user_id"].ToString();

                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic["access_token"] = token;
                dic["user_ids"] = userId;
                dic["fields"] = "uid,first_name,last_name,sex,photo_100";
                var request = new OAuth2Request ("GET", new Uri (Constants.UserInfoUrl), dic, e.Account);
				var response = await request.GetResponseAsync ();
				if (response != null) {
					// Deserialize the data and store it in the account store
					// The users email address will be used to identify data in SimpleDB
					string userJson = response.GetResponseText ();
                    ResponseUsers responseUsers = JsonConvert.DeserializeObject<ResponseUsers> (userJson);
                    User user = responseUsers.users.First();
                    if (user != null) {
                        e.Account.Username = user.FirstName + " " + user.LastName;
                        AccountStore.Create(Context).Save(e.Account, App.AppName);
                        App.User = user;
                    }
				}
			}
			// If the user is logged in navigate to the TodoList page.
			// Otherwise allow another login attempt.
			App.SuccessfulLoginAction.Invoke ();
		}
	}
}
