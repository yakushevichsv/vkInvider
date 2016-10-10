using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

using GroupMemberItem = VKPeopleInviter.GroupDetectionMultipleItemSelection<VKPeopleInviter.User>;

namespace VKPeopleInviter
{
	public partial class PeopleInvitationStatusPage : ContentPage
	{
		VKManager vkManager = VKManager.sharedInstance();
		GroupMemberItem[] m_Users = null;
		bool doneOnce = false;
		string groupId;
		ToolbarItem m_CancelOpt = null;

		public PeopleInvitationStatusPage(GroupMemberItem[] users, string groupId) : base()
		{
			InitializeComponent();
			this.m_Users = users;
			this.groupId = groupId;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (!doneOnce && m_Users != null)
			{
				doneOnce = true;
				PeopleListView.ItemsSource = m_Users;
				SendButton.IsVisible = m_Users.Length != 0;
				m_Users = null;
			}
		}


		void Handle_ItemAppearing(object sender, ItemVisibilityEventArgs e)
		{
			var itemSelection = (GroupMemberItem)e.Item;
			if (itemSelection.isDetecting)
				return;

			itemSelection.Status = VKManager.UserGroupStatus.Detecting;
			HandleRowWithUserAppear(itemSelection);
		}

		void Handle_ItemDisappearing(object sender, Xamarin.Forms.ItemVisibilityEventArgs e)
		{
			var itemSelection = (GroupMemberItem)e.Item;
			if (itemSelection.isDetecting)
				itemSelection.Status = VKManager.UserGroupStatus.Cancelled;
			CancelRowWithUserAppearance(itemSelection.Item);
		}

		#region Refreshing 

		private CancellationTokenSource m_RefreshTokenSource;

		void Handle_Refreshing(object sender, System.EventArgs e)
		{
			Handle_CancelOperation();
			var failedItems = new List<GroupMemberItem>();

			foreach (var item in PeopleListView.ItemsSource)
			{
				var groupMemberItem = item as GroupMemberItem;

				if (groupMemberItem.isDetectionNeeded)
				{
					failedItems.Add(groupMemberItem);
					groupMemberItem.Status = VKManager.UserGroupStatus.Detecting;
				}
			}

			if (failedItems.Count == 0)
			{
				PeopleListView.EndRefresh();
				return;
			}

			m_CancelOpt = new ToolbarItem("Cancel", null, Handle_CancelOperation, ToolbarItemOrder.Primary);
			ToolbarItems.Add(m_CancelOpt);

			if (!PeopleListView.IsRefreshing)
				PeopleListView.BeginRefresh();

			var tasks = new List<Task>(1);//new List<Task>(failedItems.Count);

			var cancellationTokenSource = new CancellationTokenSource();
			m_RefreshTokenSource = cancellationTokenSource;

			Task.Factory.StartNew((arg) =>
			{

				try
				{
					tasks.Add(DetectGroupStatusForUsers(failedItems.ToArray()));

					/*foreach (var item in failedItems)
					{
						tasks.Add(this.HandleRowWithUserAppear(item));
					}*/
					Task.WaitAll(tasks.ToArray());
				}
				catch (Exception exp)
				{
					Debug.WriteLine("Invitation Status Handle reflection Error\n " + exp);
					var canceled = exp is AggregateException;
					Device.BeginInvokeOnMainThread(() =>
						{
							foreach (var item in failedItems)
							{
								if (item.isDetecting)
									item.Status = canceled ? VKManager.UserGroupStatus.Cancelled : VKManager.UserGroupStatus.Failed;
							}
						});

				}
				finally
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						TerminateRefreshing();
					});
				}

			}, cancellationTokenSource.Token);

		}

		private void TerminateRefreshing()
		{
			if (PeopleListView.IsRefreshing)
				PeopleListView.EndRefresh();
			Handle_CancelOperation();
		}

		void Handle_CancelOperation()
		{
			if (m_RefreshTokenSource != null)
			{
				m_RefreshTokenSource.Cancel();
				m_RefreshTokenSource = null;
				if (ToolbarItems.Count != 0)
					ToolbarItems.RemoveAt(0);
			}
		}

		#endregion

		#region Group Invitation

		async void Handle_InviteToGroup(object sender, System.EventArgs e)
		{
			if (!SendButton.IsEnabled)
				return;

			SendButton.IsEnabled = false;

			Debug.WriteLine("Inviting to group method");

			//TODO: Add some activity indicator view..

			foreach (var item in PeopleListView.ItemsSource)
			{
				var groupMemberItem = item as GroupMemberItem;

				//groupMemberItem.Status = 
				Debug.WriteLine("Inviting user to the group with id " + groupMemberItem.Item.Id);
				try
				{

					var result = await vkManager.InviteUserToAGroup(groupMemberItem.Item.Id, this.groupId);
					groupMemberItem.Status = result == 1 ? VKManager.UserGroupStatus.Invited : VKManager.UserGroupStatus.Failed;
					Debug.WriteLine("Status of user with id " + groupMemberItem.Item.Id + " invitation " + groupMemberItem.Status);

				}
				catch (Exception exp)
				{
					Debug.WriteLine("Invitation Exception " + exp);
					var cancelled = exp is OperationCanceledException;

					if (groupMemberItem.Status != VKManager.UserGroupStatus.Invited)
						groupMemberItem.Status = cancelled ? VKManager.UserGroupStatus.Cancelled : VKManager.UserGroupStatus.Failed;

					if (cancelled)
						break;
					//TODO: analyze error...
				}

				SendButton.IsEnabled = true;
			}
		}

		#endregion

		#region detecting Group belongance

		private void CancelRowWithUserAppearance(User user)
		{
			Debug.WriteLine("CancelRowWithUserAppearance");

			vkManager.CancelIsAGroupMemberDetection(new string[] { user.Id }, groupId);
		}

		private void HandleRowWithUserAppear(GroupMemberItem user)
		{
			DetectGroupStatusForUsers(new List<GroupMemberItem> { user }.ToArray());
		}

		private async Task DetectGroupStatusForUsers(GroupMemberItem[] users)
		{
			Debug.WriteLine("DetectGroupStatusForUsers");
			VKManager.UserGroupStatus[] statuses = null;

			try
			{
				var ids = users.Select((arg) => arg.Item.Id).ToArray();
				var result = await vkManager.DetectIfUserIsAGroupMember(ids, groupId);

				statuses = result;
				Debug.WriteLine("DetectGroupStatusForUsers finished");
			}
			catch (VKOperationException exp)
			{
				Debug.WriteLine("VK Oper error HadleRowWithUserAppear : Exception " + exp);
			}
			catch (Exception exp)
			{
				Debug.WriteLine("HadleRowWithUserAppear : Exception " + exp);
			}
			finally
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					var i = 0;
					foreach (var user in users)
					{
						var status = statuses!= null ? statuses[i] : VKManager.UserGroupStatus.Failed;
						user.Status = status;
						i++;
					}
				});
			}
		}
		#endregion
	}
}
