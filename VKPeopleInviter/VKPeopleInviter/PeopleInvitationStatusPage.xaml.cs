using System;
using System.Diagnostics;
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
			itemSelection.isDetectingStatus = true;
			HandleRowWithUserAppear(itemSelection);
		}

		void Handle_ItemDisappearing(object sender, Xamarin.Forms.ItemVisibilityEventArgs e)
		{
			var itemSelection = (GroupMemberItem)e.Item;
			itemSelection.isDetectingStatus = false;
			CancelRowWithUserAppearance(itemSelection.Item);
		}

		private void CancelRowWithUserAppearance(User user)
		{
			Debug.WriteLine("CancelRowWithUserAppearance");

			vkManager.CancelIsAGroupMemberDetection(new string[] { user.Id }, groupId);
		}

		private async Task HandleRowWithUserAppear(GroupMemberItem user)
		{
			Debug.WriteLine("HadleRowWithUserAppear");

			try
			{
				var result = await vkManager.DetectIfUserIsAGroupMember(new string[] { user.Item.Id }, groupId);

				var status = result[0];
				user.Status = status;
				user.isDetectingStatus = false;
				Debug.WriteLine("HadleRowWithUserAppear finished");
			}
			catch (VKOperationException exp)
			{
				Debug.WriteLine("HadleRowWithUserAppear : Exception " + exp);
				user.Status = VKManager.UserGroupStatus.Failed;
			}
			catch (Exception exp)
			{
				Debug.WriteLine("HadleRowWithUserAppear : Exception " + exp);
			}
		}
	}
}
