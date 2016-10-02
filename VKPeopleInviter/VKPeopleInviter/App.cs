using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VKPeopleInviter
{
    public class App : Application
    {
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
                return new Action(() => {
					Current.MainPage.Navigation.PopModalAsync();
                    if (IsLoggedIn)
					{
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

			var settings = new SettingsPage();
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
