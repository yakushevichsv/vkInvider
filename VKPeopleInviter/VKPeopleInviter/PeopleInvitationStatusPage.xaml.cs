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

			m_CancelOpt = new ToolbarItem("Cancel",null,Handle_CancelOperation,ToolbarItemOrder.Primary);
			ToolbarItems.Add(m_CancelOpt);

				if (!PeopleListView.IsRefreshing)
					PeopleListView.BeginRefresh();

			var tasks = new List<Task>(1);//new List<Task>(failedItems.Count);

				var cancellationTokenSource = new CancellationTokenSource();
				m_RefreshTokenSource = cancellationTokenSource;

				Task.Factory.StartNew((arg) => {

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
					var canceled = exp is AggregateException
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

				} ,cancellationTokenSource.Token);

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
			VKManager.UserGroupStatus[] statuses = new VKManager.UserGroupStatus[users.Length];

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
				Device.BeginInvokeOnMainThread(() => {
					var i = 0;
					foreach (var user in users)
					{
						user.Status = statuses.Length != 0 ? statuses[i] : VKManager.UserGroupStatus.Failed;
						i++;
					}
				});
			}
		}
		#endregion
	}
}
