using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	public partial class InvitePeopleToGroup : ContentPage
	{

		void Handle_SearchButtonPressed(object sender, System.EventArgs e)
		{
			var text = ((SearchBar)sender).Text;
			searchPrivate(text);
		}


		void Handle_TextChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
		{
			searchPrivate(e.NewTextValue);
		}

		private async void searchPrivate(string text)
		{	
			if (string.IsNullOrWhiteSpace(text))
			{
				//Search all people...
			}
			else {
				try
				{
					PeopleListView.BeginRefresh();
					var result = await VKManager.sharedInstance().SearchPeople(text);

					PeopleListView.ItemsSource = result;
				}
				catch (Exception error)
				{
					//TODO: analyze too many request...
					Debug.WriteLine("Error " + error.ToString());

					//var alert = DisplayAlert("Error", error.ToString(), "Cancel");
				}
				finally
				{
					PeopleListView.EndRefresh();
				}
			}
		}

		void Handle_Clicked(object sender, System.EventArgs e)
		{
			throw new NotImplementedException();
		}

		void Handle_ItemTapped(object sender, Xamarin.Forms.ItemTappedEventArgs e)
		{
			throw new NotImplementedException();
		}

		void Handle_Refreshing(object sender, System.EventArgs e)
		{
			//lthrow new NotImplementedException();
		}

		public InvitePeopleToGroup()
		{
			InitializeComponent();

			//StackLayout layout = new StackLayout();
			//layout.WidthRequest
		}
	}
}

