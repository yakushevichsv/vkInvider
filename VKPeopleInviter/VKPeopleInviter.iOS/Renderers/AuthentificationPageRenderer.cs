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
		bool forced = false;

		UIKit.UIViewController authVC = null;

		private void performAuthentification()
		{
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
		}	

		protected override async void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);
		
			AccountStore store = AccountStore.Create();
			Account fAccount = null;
			try
			{
				List<Account> accounts; //store.FindAccount(App.AppName);
				accounts = await store.FindAccountsForServiceAsync(App.AppName);
				fAccount = accounts != null ? accounts.First() : null;
			}
			catch (Exception exp)
			{
				Console.WriteLine("Exception " + exp.ToString());
			}

			if (fAccount == null || forced) {

				if (!isShown)
				{
					isShown = true;
					performAuthentification();
			}
			}
			else {
				if (!isShown)
				{
					string[] results = fAccount.Username.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					String tokenString = String.Empty;
					fAccount.Properties.TryGetValue("token", out tokenString);

					String expiration;
					DateTime expirationDate = DateTime.Now;
					Double diff = 0;
					String userId = fAccount.Properties["userID"];
					if (fAccount.Properties.TryGetValue("expiration", out expiration) && DateTime.TryParse(expiration, out expirationDate) && !String.IsNullOrEmpty(userId))
					{
						TimeSpan spanDiff = DateTime.Now.Subtract(expirationDate);
						diff = spanDiff.TotalSeconds;
						if (diff > 0.0 && !forced)
						{
							VKManager.sharedInstance().didAuthorizeWithToken(tokenString, (int)Convert.ToInt64(userId), (int)diff);

							App.User.FirstName = results.First();
							App.User.LastName = results.Last();
							App.User.ExpirationDate = expirationDate;
							App.User.Token = tokenString;
							//App.MoveToItemsSelectionPage();
							App.SuccessfulLoginAction.Invoke();
							return;
						}
					}
					else {
						//Wrong account...
						store.Delete(fAccount, App.AppName);
					}
					isShown = false;
					performAuthentification();
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
				Int32 duration = Convert.ToInt32(e.Account.Properties["expires_in"]);
				Dictionary<string, string> dic = new Dictionary<string, string>();
				dic["access_token"] = token;
				dic["user_ids"] = userId;

				dic["fields"] = "uid,first_name,last_name,sex,photo_100";

				VKManager.sharedInstance().didAuthorizeWithToken(token, (int)Convert.ToInt64(userId), (int)duration);

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
						e.Account.Properties.Add("token", token);
						e.Account.Properties.Add("userID", userId);

						DateTime current = DateTime.Now;
						TimeSpan value = new TimeSpan(0, 0, duration);
						DateTime current2 = current.Add(value);

						e.Account.Properties.Add("expiration", current2.ToString());
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

