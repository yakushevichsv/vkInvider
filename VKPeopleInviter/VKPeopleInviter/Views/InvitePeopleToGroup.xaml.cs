using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using System.Linq;

namespace VKPeopleInviter
{
	public partial class InvitePeopleToGroup : ContentPage
	{
		private string searchText = "";
		private int count = 0;
		private int offset = 0;
		bool cancelSearch = false;
		const int c_OrigCount = 8;

		void Handle_ItemAppearing(object sender, Xamarin.Forms.ItemVisibilityEventArgs e)
		{

			var source = (List<VKPeopleInviter.MultipleItemSelectlon<User>>)((ListView)sender).ItemsSource;

			var Item = (MultipleItemSelectlon<User>)e.Item;


			var isLast = source.Count != 0 && (source[source.Count - 1].Item.Id).Equals(((MultipleItemSelectlon<User>)e.Item).Item.Id);

			if (isLast)
				SearchPrivate(searchText, count + offset, 100);
		}

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
					MultipleItemSelectlon<User> user = (MultipleItemSelectlon<User>)((ListView)sender).SelectedItem;
					user.Selected = false;
				}

				SendButton.IsVisible = GetSelection().Count != 0;

				return;
			}

			MultipleItemSelectlon<User> selUser = (MultipleItemSelectlon<User>)e.SelectedItem;

			selUser.Selected = !selUser.Selected;
			SendButton.IsVisible = GetSelection().Count != 0;
			((ListView)sender).SelectedItem = null;
		}


		public List<User> GetSelection()
		{
			var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;
			return source.Where(item => item.Selected).Select(wrappedItem => wrappedItem.Item).ToList();
		}

		void Handle_SearchButtonPressed(object sender, System.EventArgs e)
		{
			var text = ((SearchBar)sender).Text;
			SearchPrivate(text);
		}


		void Handle_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (cancelSearch && e.OldTextValue == null)
			{
				cancelSearch = false;
				return;
			}

			var text = e.NewTextValue;
			var sText = e.OldTextValue;

			if (text == null)
			{
				cancelSearch = true;
				((SearchBar)sender).Text = sText;
				VKManager.sharedInstance().CancelSearchPeople(sText);
				PeopleListView.EndRefresh();

				return;
			}

			var useMinimum = false;
			if (sText != text && sText != null && sText.Length != 0)
			{

				VKManager.sharedInstance().CancelSearchPeople(sText);
				PeopleListView.EndRefresh();
				offset = 0;
				count = 0;
				//arrayOfIds.Clear();
				useMinimum = true;
				PeopleListView.ItemsSource = null;
			}

			if (text == null)
			{
				cancelSearch = true;
				((SearchBar)sender).Text = sText;
				return;
			}

			((SearchBar)sender).Text = text;
			SearchPrivate(text, offset, useMinimum ? c_OrigCount : 100);
		}

		private async void SearchPrivate(string text, int offset2 = 0, int count2 = 100)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				//Search all people...
			}
			else {
				try
				{
					PeopleListView.BeginRefresh();
					int cityCode = 5835; // Rechica...
					var result = await VKManager.sharedInstance().SearchPeople(text, cityCode, offset2, count2);
					var finalResult = new List<MultipleItemSelectlon<User>>();

					this.searchText = text;
					this.offset = offset2;
					if (result.Count != 0)
						this.count = result.Count;

					var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;

					if (source != null && source.Count != 0)
						finalResult.AddRange(source);


					foreach (var currentUser in result)
						finalResult.Add(new MultipleItemSelectlon<User>() { Selected = false, Item = currentUser });


					PeopleListView.ItemsSource = finalResult;
				}
				catch (UsersNotFoundException error)
				{
					Debug.WriteLine("Users not found " + error); 
					await DisplayAlert("Error ", error.ToString(), "Cancel");
				}
				catch (OperationCanceledException error)
				{
					Debug.WriteLine("Cancellation " + error);
				}
				catch (Exception error)
				{
					//TODO: analyze too many request...
					Debug.WriteLine("Error " + error);

					//var alert = DisplayAlert("Error", error.ToString(), "Cancel");
				}
				finally
				{
					PeopleListView.EndRefresh();
				}
			}
		}

		async void Handle_SendClicked(object sender, EventArgs e)
		{
			try
			{
				var ids = GetSelection().Select(item => item.Id).ToArray();
				var settingsManager = new SettingsManager(Application.Current);
				var result = await VKManager.sharedInstance().SendMessageToUsers(settingsManager.InvitationText, ids);
				Debug.WriteLine("Result " + result);
				//analayze results of sending...
			}
			catch (Exception error)
			{
				Debug.WriteLine("Error" + error.ToString());
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

