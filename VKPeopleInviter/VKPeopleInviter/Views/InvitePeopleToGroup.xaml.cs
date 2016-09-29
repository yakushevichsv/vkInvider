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
			
			var source = (List<VKPeopleInviter.MultipleItemSelectlon<User>>) ((ListView)sender).ItemsSource;

			var Item = (MultipleItemSelectlon<User>)e.Item;

			Item.Selected = arrayOfIds.Contains(Item.Item.Id);

			var isLast = source.Count != 0 && (source[source.Count - 1].Item.Id).Equals(((MultipleItemSelectlon<User>)e.Item).Item.Id);

			if (isLast) 
				SearchPrivate(searchText, count + offset, 100);
		}

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
					MultipleItemSelectlon<User> user = (MultipleItemSelectlon<User>)((ListView)sender).SelectedItem;
					var index = arrayOfIds.IndexOf(user.Item.Id);

					if (index != Int32.MaxValue)
					{
						arrayOfIds.RemoveAt(index);
					}
				}

				SendButton.IsVisible = arrayOfIds.Count != 0;

				return;
			}

			MultipleItemSelectlon<User> selUser = (MultipleItemSelectlon<User>)e.SelectedItem;
			var selected = false;
			if (arrayOfIds.Contains(selUser.Item.Id))
			{
				arrayOfIds.Remove(selUser.Item.Id);
			}
			else {
				selected = true;
				arrayOfIds.Add(selUser.Item.Id);
			}
			selUser.Selected = selected;
			SendButton.IsVisible = arrayOfIds.Count != 0;
			//((ListView)sender).SelectedItem = null;
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
				arrayOfIds.Clear();
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
							var result = await VKManager.sharedInstance().SearchPeople(text, offset2, count2);
					var finalResult = new List<MultipleItemSelectlon<User>>();

					this.searchText = text;
					this.offset = offset2;
					this.count = result.Count;

					var source = (List<VKPeopleInviter.MultipleItemSelectlon<User>>)PeopleListView.ItemsSource;

					if (source != null && source.Count != 0) 
						finalResult.AddRange(source);
						

					foreach (var currentUser in result)
						finalResult.Add(new MultipleItemSelectlon<User>() { Selected = false, Item = currentUser });
					

					PeopleListView.ItemsSource = finalResult;
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

