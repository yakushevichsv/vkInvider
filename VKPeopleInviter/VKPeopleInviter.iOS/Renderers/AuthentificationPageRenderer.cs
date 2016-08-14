using System;
using Xamarin.Forms.Platform.iOS;
using Accounts;
using Xamarin.Auth;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xamarin.Forms;
using VKPeopleInviter;
using VKPeopleInviter.iOS;

[assembly: ExportRenderer(typeof(AuthenticationPage), typeof(AuthentificationPageRenderer))]

namespace VKPeopleInviter.iOS
{
	public class AuthentificationPageRenderer: PageRenderer
	{
		bool isShown = false;
		UIKit.UIViewController authVC = null;

		protected override void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);
		
			AccountStore store = AccountStore.Create();
			Account fAccount = store.FindAccountsForService(App.AppName).First(); //store.FindAccount(App.AppName);

			if (fAccount == null) {

				if (!isShown)
				{
					isShown = true;

					// Initialize the object that communicates with the OAuth service
					var auth = new OAuth2Authenticator(
								   Constants.ClientId,
								   Constants.Scope,
								   new Uri(Constants.AuthorizeUrl),
								   new Uri(Constants.RedirectUrl));
					auth.AllowCancel = true;

					// Register an event handler for when the authentication process completes
					auth.Completed += OnAuthenticationCompleted;

					var vc = auth.GetUI();
					authVC = vc;
					AddChildViewController(vc);
					View.AddSubview(vc.View);
					vc.DidMoveToParentViewController(this);    
					//PresentViewController(vc, true, null);
				// Display the UI
				//var activity = Context as Activity;
				//activity.StartActivity(auth.GetUI(activity));
			}
			}
			else {
				if (!isShown)
				{
					string[] results = fAccount.Username.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					App.User.FirstName = results.First();
					App.User.LastName = results.Last();
					//App.MoveToItemsSelectionPage();
					App.SuccessfulLoginAction.Invoke();
				}
			}

		}

		async void OnAuthenticationCompleted(object sender, AuthenticatorCompletedEventArgs e)
		{
			//DismissViewController(true, null);
			if (e.IsAuthenticated)
			{

				if (authVC != null)
				{
					authVC.View.RemoveFromSuperview();
					authVC.RemoveFromParentViewController();
					authVC = null;
				}
				// If the user is authenticated, request their basic user data from Google
				// UserInfoUrl = https://www.googleapis.com/oauth2/v2/userinfo
				//Console.WriteLine(e.Account.ToString());
				string token = e.Account.Properties["access_token"].ToString();
				string userId = e.Account.Properties["user_id"].ToString();

				Dictionary<string, string> dic = new Dictionary<string, string>();
				dic["access_token"] = token;
				dic["user_ids"] = userId;
				dic["fields"] = "uid,first_name,last_name,sex,photo_100";
				var request = new OAuth2Request("GET", new Uri(Constants.UserInfoUrl), dic, e.Account);
				var response = await request.GetResponseAsync();
				if (response != null)
				{
					// Deserialize the data and store it in the account store
					// The users email address will be used to identify data in SimpleDB
					ResponseUsers responseUsers = JsonConvert.DeserializeObject<ResponseUsers>(response.GetResponseText());
					User user = responseUsers.users.First();
					if (user != null)
					{
						e.Account.Username = user.FirstName + " " + user.LastName;
						AccountStore store = AccountStore.Create();
						store.Save(e.Account, App.AppName);
						App.User = user;
					}
				}
			}

			// If the user is logged in navigate to the TodoList page.
			// Otherwise allow another login attempt.
			/*Device.BeginInvokeOnMainThread(() =>
			{
				App.SuccessfulLoginAction.Invoke();
			});*/

			App.MoveToItemsSelectionPage();
		}
	}
}

