using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VKPeopleInviter
{
    public class App : Application
    {
		void Tabbed_CurrentPageChanged(object sender, EventArgs e)
		{
			var tabbed = MainPage as TabbedPage;
			if (tabbed != null)
			{
				if (tabbed.CurrentPage.Title == "Profile")
				{
					var profilePage = (tabbed.CurrentPage as NavigationPage).CurrentPage as UserProfilePage;
					profilePage.userProfile = new UserProfile(User);
				}
			}
		}

		public static string AppName { get { return "VKPeopleInviter"; } }

        //public static TodoItemManager TodoManager { get; private set; }

        public static User User { get; set; }

        //public static ITextToSpeech Speech { get; set; }

        static NavigationPage NavPage;

        public static bool IsLoggedIn
        {
            get
			{
				//return false;
                if (User != null)
                    return !(string.IsNullOrWhiteSpace(User.FirstName) && string.IsNullOrWhiteSpace(User.LastName));
                else
                    return false;
            }
        }

        public static Action SuccessfulLoginAction
        {
            get
            {
				Contract.Ensures(Contract.Result<Action>() != null);
				return new Action(() => {
					//Current.MainPage.Navigation.PopModalAsync();
					NavPage.Navigation.PopModalAsync(false);
					if (IsLoggedIn)
					{
						var tabbed = Current.MainPage as TabbedPage;
						if (tabbed != null && tabbed.Children.Count != 3)
						{
							var profilePage = new UserProfilePage();
							var profile = new NavigationPage(profilePage);
							profile.Title = "Profile";
							tabbed.Children.Add(profile);
							tabbed.CurrentPageChanged += (Current as App).Tabbed_CurrentPageChanged;
						}
						//Current.MainPage.Navigation.PushModalAsync(new ItemsSelectorPage());
						NavPage.PushAsync(new ItemsSelectorPage());
					}
                });
            }
        }

        public App()
        {
			User = new User();

			var tabbedPage = new TabbedPage();

			var navPage = new NavigationPage(new LoginPage());
			navPage.Title = "Actions";

			tabbedPage.Children.Add(navPage);

			var settings = new NavigationPage(new SettingsPage());
			settings.Title = "Settings";

			tabbedPage.Children.Add(settings);
			NavPage = navPage;

			//tabbedPage.
            //NavPage = new NavigationPage(new LoginPage());
			MainPage = tabbedPage;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
