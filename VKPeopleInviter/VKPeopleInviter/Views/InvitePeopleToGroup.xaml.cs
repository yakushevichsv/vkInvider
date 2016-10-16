using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using System.Linq;
using VKPeopleInviter.Controls;

namespace VKPeopleInviter
{
	public partial class InvitePeopleToGroup : ContentPage
	{
		private string searchText = "";
		private int count = 0;
		private int offset = 0;
		bool cancelSearch = false;
		const int c_OrigCount = 8;
		VKManager vkManager = VKManager.sharedInstance();

		async void Handle_ItemAppearing(object sender, ItemVisibilityEventArgs e)
		{
			var ItemWrapper = (MultipleItemSelectlon<User>)e.Item;
			var id = ItemWrapper.Item.Id;
			var source = (List<MultipleItemSelectlon<User>>)((ListView)sender).ItemsSource;
			try
			{
				Debug.WriteLine("DetectFriendshipStatusWithUsers");
				var result = await vkManager.DetectFriendshipStatusWithUsers(new string[] { id }); //TODO: perform request in a group..
				Debug.WriteLine("DetectFriendshipStatusWithUsers finished with result " + result);

				foreach (var keyPair in result)
				{
					var fWrapper =  source.Find((item) => item.Item.Id == keyPair.Key);
					if (fWrapper == null)
						continue;
					Debug.WriteLine("DetectFriendshipStatusWithUsers Detected status for item " + fWrapper.Item + "\n Status " + keyPair.Value);
				}
			}
			catch (Exception exp)
			{
				var vkExp = exp as VKOperationException;
				if (vkExp != null)
				{
					
				}
				Debug.WriteLine("DetectFriendshipStatusWithUsers Detect friendship state " + exp);
			}


			var isLast = source.Count != 0 && (source[source.Count - 1].Item.Id).Equals(((MultipleItemSelectlon<User>)e.Item).Item.Id);

			if (isLast)
				SearchPrivate(searchText, count + offset, 100);
		}

		void Handle_ItemDisappearing(object sender, ItemVisibilityEventArgs e)
		{
			var ItemWrapper = (MultipleItemSelectlon<User>)e.Item;
			var id = ItemWrapper.Item.Id;
			vkManager.CancelFriendshipDetection(new string[] { id });
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			SearchPeople.TextColor = Color.Black;

			//TODO: remove that...
			var testResult = vkManager.GroupsGetMembers(Constants.Group1ToUseId);
			Debug.WriteLine(" Group Members result " + testResult);
		}

		void Handle_SelectUnSelectAll(object sender, System.EventArgs e)
		{
			var item = (ToolbarItem)sender;
			bool select = item.Text.StartsWith("Select",StringComparison.OrdinalIgnoreCase);
			ChangeSelectionState(select);

			if (!select)
				item.Text = "Select All";
			else
				item.Text = " Unselect All";
		}

		private void ChangeSelectionState(bool selected)
		{
			var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;

			if (source == null)
				return;

			foreach (var item in source)
				item.Selected = selected;

			SendButton.IsVisible = GetSelection().Count != 0;
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

		void Handle_ItemTapped(object sender, Xamarin.Forms.ItemTappedEventArgs e)
		{
			Handle_ItemSelected(sender, new SelectedItemChangedEventArgs(e.Item));
		}



		public List<User> GetSelection()
		{
			var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;
			if (source == null)
				return new List<User>();
			var result = source.Where(item => item.Selected).Select(wrappedItem => wrappedItem.Item).ToList();
			return result;
		}

		private void RunActivityIndicator()
		{
			if (!ActivityIndicator.IsRunning && !PeopleListView.IsRefreshing)
			{
				ActivityIndicator.IsVisible = true;
				ActivityIndicator.IsRunning = true;
			}
		}

		private void StopActivityIndicator()
		{
			if (ActivityIndicator.IsRunning)
			{
				ActivityIndicator.IsVisible = false;
				ActivityIndicator.IsRunning = false;
			}
		}

		void Handle_SearchButtonPressed(object sender, EventArgs e)
		{
			var text = ((SearchBar)sender).Text;
			searchText = text;
			RunActivityIndicator();
			SearchPrivate(text, offset, count);
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
				vkManager.CancelOperation(sText);
				PeopleListView.EndRefresh();

				return;
			}

			var useMinimum = false;
			if (sText != text && sText != null && sText.Length != 0)
			{

				vkManager.CancelOperation(sText);
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
			//RunActivityIndicator();
			searchText = text;
			count = useMinimum ? c_OrigCount : 100;
			PeopleListView.BeginRefresh();
		}




		private async void SearchPrivate(string text, int offset2 = 0, int count2 = 100)
		{
			Debug.WriteLine("Search Private Text: " + text + "Offset " + offset2 + " Count " + count2);
			if (string.IsNullOrWhiteSpace(text))
			{
				//Search all people...
				Debug.Assert(false);
			}
			else {
				try
				{
					int cityCode = 5835; // Rechica...
					var result = await vkManager.SearchPeople(text, cityCode, offset2, count2);
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
					var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;
					if (source == null || source.Count == 0)
						await DisplayAlert("Error ", error.Message, "Cancel");
					//else  if (source != null && source.Count != 0)
					//PeopleListView.ScrollTo(source.Last(), ScrollToPosition.End, true);
					Debug.WriteLine("Users not found " + error);
				}
				catch (OperationCanceledException )
				{
					Debug.WriteLine("Search Canceled! ");
				}
				catch (Exception error)
				{
					//TODO: analyze too many request...
					Debug.WriteLine("Error " + error);

					//var alert = DisplayAlert("Error", error.ToString(), "Cancel");
				}
				finally
				{
					if (PeopleListView.IsRefreshing)
						PeopleListView.EndRefresh();
					StopActivityIndicator();
					Debug.WriteLine("Search Private Finished");
				}
			}
		}

		async void Handle_SendClicked(object sender, EventArgs e)
		{
			try
			{
				Debug.WriteLine("Handle_SendClicked");
				var ids = GetSelection().Select(item => item.Id).ToArray();
				var settingsManager = new SettingsManager(Application.Current);
				await vkManager.SendMessageToUsers(settingsManager.InvitationText, ids);
				//analayze results of sending...
				await DisplayAlert("Success", "All users were notified", "OK");
				Debug.WriteLine("Success", "All users were notified");
			}
			catch (VKOperationException error)
			{
				Debug.WriteLine("Error" + error);
				await DisplayAlert("Error", error.Message, "Cancel");
			}
			catch (Exception error)
			{
				Debug.WriteLine("Error" + error);
			}
		}

		void Handle_Refreshing(object sender, System.EventArgs e)
		{
			SearchPrivate(this.searchText, offset, count);
		}

		void HandleNextToolBarClicked(object sender, EventArgs e)
		{
			var list = GetSelection();
			if (list.Count != 0)
			{
				var result = new List<GroupDetectionMultipleItemSelection<User>>();
				foreach (var arrayElemenet in list) {
					var newItem = new GroupDetectionMultipleItemSelection<User>();
					newItem.Item = arrayElemenet;
					result.Add(newItem);
				}
				var page = new PeopleInvitationStatusPage(result.ToArray(), Constants.GroupId);
				Navigation.PushAsync(page);
			}
		}

		public InvitePeopleToGroup()
		{
			InitializeComponent();
		}

		void Handle_ClickListener(UserSelectableCell m, EventArgs e)
		{
			SendButton.IsVisible = GetSelection().Count != 0;
		}
	}
}

