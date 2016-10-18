using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace VKPeopleInviter
{
	public partial class ItemsSelectorPage : ContentPage
	{
		async void Handle_Clicked(object sender, System.EventArgs e)
		{
			await Navigation.PushAsync(new InvitePeopleToGroup());
		}

		async void HandleGroupsUse(object sender, System.EventArgs e)
		{
			await Navigation.PushAsync(new InvitePeopleFromGroupToGroup());
		}

		public ItemsSelectorPage()
		{
			InitializeComponent();
		}
	}
}

