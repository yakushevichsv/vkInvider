﻿using Android.Webkit;
using System.Linq;
using VKPeopleInviter.Droid;
using Xamarin.Auth;

[assembly: Xamarin.Forms.Dependency (typeof(Authentication))]

namespace VKPeopleInviter.Droid
{
	public class Authentication : IAuthentication
	{
		public void Logout ()
		{
			CookieManager.Instance.RemoveAllCookie ();

			var accounts = AccountStore.Create (Android.App.Application.Context).FindAccountsForService (App.AppName);
			var account = accounts.FirstOrDefault ();

			if (account != null) {
				AccountStore.Create (Android.App.Application.Context).Delete (account, App.AppName);
				App.User = null;
			}
		}
	}
}
