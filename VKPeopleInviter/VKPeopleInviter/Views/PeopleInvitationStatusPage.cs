using System;

using Xamarin.Forms;

namespace VKPeopleInviter
{
	public class PeopleInvitationStatusPage : ContentPage
	{
		public PeopleInvitationStatusPage()
		{
			Content = new StackLayout
			{
				Children = {
					new Label { Text = "Hello ContentPage" }
				}
			};
		}
	}
}

