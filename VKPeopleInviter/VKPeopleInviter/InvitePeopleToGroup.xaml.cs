using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	public partial class InvitePeopleToGroup : ContentPage
	{
		private List<string> arrayOfIds = new List<String>();

		protected override void OnAppearing()
		{
			base.OnAppearing();
			SearchPeople.TextColor = Color.Black;
		}

		void Handle_ItemSelected(object sender, Xamarin.Forms.SelectedItemChangedEventArgs e)
		{
			if (e.SelectedItem == null)
			{
				if (((ListView)sender).SelectedItem != null)
				{
					User user = (User)((ListView)sender).SelectedItem;
					var index = arrayOfIds.IndexOf(user.Id);

					if (index != Int32.MaxValue) {
						arrayOfIds.RemoveAt(index);
					}
				}

				SendButton.IsVisible = arrayOfIds.Count != 0;

				return;
			}

			User selUser = (User)e.SelectedItem;

			if (arrayOfIds.Contains(selUser.Id))
			{
				arrayOfIds.Remove(selUser.Id);
			}
			else {
				arrayOfIds.Add(selUser.Id);
			}

			SendButton.IsVisible = arrayOfIds.Count != 0;
			//((ListView)sender).SelectedItem = null;
		}

		void Handle_SearchButtonPressed(object sender, System.EventArgs e)
		{
			var text = ((SearchBar)sender).Text;
			searchPrivate(text);
		}


		void Handle_TextChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
		{
			var text = e.NewTextValue;

			if (text == null)
			{
				//Cancel button was pressed....
				PeopleListView.EndRefresh();
				return;
			}

			((SearchBar)sender).Text = text;
			searchPrivate(text);
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

		async void Handle_SendClicked(object sender, System.EventArgs e)
		{
			try
			{
				var result = await VKManager.sharedInstance().SendMessageToUsers("Привет Катюха \n\r Читай тут бай ?" + "http://www.tut.by", arrayOfIds.ToArray());
				Debug.WriteLine("Result " + result);
				//analayze results of sending...
			}
			catch (Exception error)
			{
				Debug.WriteLine("Error" + error.ToString());
			}
			finally
			{
				
			}
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

