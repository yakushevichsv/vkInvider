using System;
using System.Linq;
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
					//Current.MainPage.Navigation.PopModalAsync();
                    if (IsLoggedIn)
					{
						//NavPage.PopToRootAsync();
						//Current.MainPage.Navigation.RemovePage(NavPage.Navigation.NavigationStack.First());
						Current.MainPage.Navigation.PushModalAsync(new ItemsSelectorPage());
						//Current.MainPage.Navigation.InsertPageBefore(new ItemsSelectorPage(), NavPage.Navigation.NavigationStack.First());
						//Current.MainPage.Navigation.PopToRootAsync();
						//NavPage.CurrentPage = new ItemsSelectorPage()
						//NavPage.PushAsync(new ItemsSelectorPage());
						//NavPage.Navigation.InsertPageBefore(new ItemsSelectorPage(), NavPage.Navigation.NavigationStack.First());
						//NavPage.Navigation.PushAsync(new ItemsSelectorPage());

						//NavPage.Navigation.PushAsync(new LoginPage());
						//NavPage.Navigation.PopToRootAsync();
						//NavPage.Navigation.PushAsync(new ItemsSelectorPage(
					}
                });
            }
        }


		public async static void MoveToItemsSelectionPage() 
		{
			await NavPage.CurrentPage.Navigation.PopModalAsync();
			       
			await NavPage.PushAsync(new ItemsSelectorPage());
			//await Current.MainPage.Navigation.PopModalAsync();
			//NavPage.Navigation.InsertPageBefore(new ItemsSelectorPage(), Current.MainPage.Navigation.NavigationStack.First());
			//await NavPage.PopToRootAsync();
			//await NavPage.PushAsync(new ItemsSelectorPage());
			//NavPage.PushAsync(new ItemsSelectorPage());//NaNavigation.PushAsync(new ItemsSelectorPage());
		}

        public App()
        {
			User = new User();

            NavPage = new NavigationPage(new LoginPage());
            MainPage = NavPage;
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
