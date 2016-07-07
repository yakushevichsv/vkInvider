using System;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	public partial class LoginPage : ContentPage
	{
		public LoginPage ()
		{
			InitializeComponent();
		}

		void OnLoginClicked (object sender, EventArgs e)
		{
			// Use a custom renderer to display the authentication UI
			Navigation.PushModalAsync (new AuthenticationPage ());			
		}
	}
}
