﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using System.Linq;
using VKPeopleInviter.Controls;

namespace VKPeopleInviter
{
	public partial class InvitePeopleFromGroupToGroup : ContentPage
	{
		long totalCode = -1;

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
				GetChunkOfMembersUsingRealData();
		}

		void Handle_ItemDisappearing(object sender, ItemVisibilityEventArgs e)
		{
			var ItemWrapper = (MultipleItemSelectlon<User>)e.Item;
			var id = ItemWrapper.Item.Id;
			vkManager.CancelFriendshipDetection(new string[] { id });
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

			ChangeToolBarAndSend();
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
				ChangeToolBarAndSend();
				return;
			}

			MultipleItemSelectlon<User> selUser = (MultipleItemSelectlon<User>)e.SelectedItem;
			selUser.Selected = !selUser.Selected;
			ChangeToolBarAndSend();
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

		private void GetChunkOfMembersUsingRealData() {
			Debug.WriteLine("Start GetChunkOfMembersUsingRealData");

			var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;

			long offset = 0;
			if (!(source == null || source.Count == 0))
				offset = source.Count;

			long count = 0;

			if (totalCode  > offset)
				count = Math.Min(100,totalCode - offset);
			else if (totalCode == offset || totalCode < 0 )
				count = 100;

			if (count != 0) {
				GetChunkOfMembers(offset, count);
			}
			else {
				//TODO: signal absence of items...
			}
			Debug.WriteLine("Ended GetChunkOfMembersUsingRealData");	
		}


		private async void GetChunkOfMembers(long offset2 = 0, long count2 = 100)
		{
			Debug.WriteLine("GetChunkOfMembers "  + "Offset " + offset2 + " Count " + count2);
				try
				{
					var result = await  vkManager.GroupsGetMembers(Constants.Group1ToUseId, offset2, count2); 
					var finalResult = new List<MultipleItemSelectlon<User>>();

					
					if (this.totalCode == 0)
						this.totalCode = result.Count;
					
					var source = (List<MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;

					if (source != null && source.Count != 0)
						finalResult.AddRange(source);

					bool selected = !LeftNavButton.Text.StartsWith("Select",StringComparison.OrdinalIgnoreCase);

					foreach (var currentUser in result.Users)
						finalResult.Add(new MultipleItemSelectlon<User>() { Selected = selected, Item = currentUser });

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

		async void Handle_SendClicked(object sender, EventArgs e)
		{
			try
			{
				Debug.WriteLine("Handle_SendClicked");
				var ids = GetSelection().Where(item => item.CanWritePrivateMessage).Select(item => item.Id).ToArray();

				if (ids.Length == 0){
					await DisplayAlert("Impossible", "All items acess only private messages", "OK");                                        
					return;
				}

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
			GetChunkOfMembersUsingRealData(); 
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
				var page = new PeopleInvitationStatusPage(result.ToArray(), Constants.GroupId.ToString());
				Navigation.PushAsync(page);
			}
		}

		public InvitePeopleFromGroupToGroup()
		{
			InitializeComponent();

			GetChunkOfMembersUsingRealData();
		}

		void Handle_ClickListener(UserSelectableCell m, EventArgs e)
		{
			ChangeToolBarAndSend();
		}

		void ChangeToolBarAndSend() 
		{
			SendButton.IsVisible = GetSelection().Count != 0;
			if (SendButton.IsVisible) 
				LeftNavButton.Text = " Unselect All";
			else 
				LeftNavButton.Text = "Select All";
		}
	}
}

