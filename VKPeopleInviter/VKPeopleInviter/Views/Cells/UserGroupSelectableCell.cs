using System;
using System.Diagnostics;
using VKPeopleInviter.Controls;
using Xamarin.Forms;

public enum UserGroupStatus { None, Member, Invited, Requested };

namespace VKPeopleInviter.Controls
{
	public class UserGroupSelectableCell : UserSelectableCell
	{
		public static readonly BindableProperty GroupMemberStatusProperty =
			BindableProperty.Create("GroupMemberStatus", typeof(VKManager.UserGroupStatus), typeof(UserGroupSelectableCell), VKManager.UserGroupStatus.None, BindingMode.TwoWay);

		public static readonly BindableProperty IsDetectingGroupMemberProperty =
			BindableProperty.Create("IsDetectingGroupMemberStatus", typeof(bool), typeof(UserGroupSelectableCell), false, BindingMode.TwoWay);


		public VKManager.UserGroupStatus GroupMemberStatus
		{
			get
			{
				return (VKManager.UserGroupStatus)GetValue(GroupMemberStatusProperty);
			}
			set
			{
				SetValue(GroupMemberStatusProperty, value);

				PerformGroupMemberStatusChange();

			}
		}

		private void PerformGroupMemberStatusChange()
		{
			IsDetectingGroupMemberStatus = GroupMemberStatus != VKManager.UserGroupStatus.None;
			lblGroupStatus.Text = IsDetectingGroupMemberStatus ? GroupMemberStatus.ToString() : "Can be invited!";
		}

		public bool IsDetectingGroupMemberStatus
		{
			get
			{
				var result = (bool)GetValue(IsDetectingGroupMemberProperty);
				return result;
			}
			set
			{
				SetValue(IsDetectingGroupMemberProperty, value);
				PerformIsDetectingGroupStatusChange();
			}
		}

		private void PerformIsDetectingGroupStatusChange()
		{
			aiIndicator.IsRunning = IsDetectingGroupMemberStatus;
		}
		
		Label lblGroupStatus;
		ActivityIndicator aiIndicator;


		public UserGroupSelectableCell() : base(false)
		{
			lblGroupStatus = new Label() { HorizontalOptions = LayoutOptions.Center };
			lblGroupStatus.FontAttributes = FontAttributes.None;
			lblGroupStatus.TextColor = Color.Black;
			aiIndicator = new ActivityIndicator();

			relativeLayout.Children.Add(aiIndicator,
										Constraint.RelativeToParent((parent) =>
					{
						return parent.Width * 0.5;
					}),
				Constraint.RelativeToParent((parent) =>
					{
						return parent.Height * 0.5;
			}), Constraint.Constant(aiIndicator.Width), Constraint.Constant(aiIndicator.Height) );

			relativeLayout.Children.Add(lblGroupStatus,
										Constraint.RelativeToParent((parent) =>
					{
						return parent.Width * 0.25;
					}),
				Constraint.RelativeToParent((parent) =>
					{
						return parent.Height * 0.5;
					})/*,Constraint.RelativeToParent((parent) =>
					{
						return parent.Height * 1;
					}),Constraint.RelativeToParent((parent) =>
					 {
						return parent.Width * 0.4;
					 }) */);
			lblGroupStatus.IsVisible = true;

			//var binding = new Binding("Status");
			//binding.Mode = BindingMode.TwoWay;
			//SetBinding(GroupMemberStatusProperty, binding);
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			if (BindingContext != null)
			{
				aiIndicator.IsRunning = IsDetectingGroupMemberStatus;
				if (!aiIndicator.IsRunning)
				{
					lblGroupStatus.Text = GroupMemberStatus.ToString();
					var obj = BindingContext as GroupDetectionMultipleItemSelection<User>;
					if (obj != null)
					{
						obj.PropertyChanged += (sender, e) =>
						{
							if (e.PropertyName == "Status")
							{
								this.GroupMemberStatus = obj.Status;
								PerformGroupMemberStatusChange();
							}
							else if (e.PropertyName == "DetectingStatus")
							{
								this.IsDetectingGroupMemberStatus = obj.isDetectingStatus;
								PerformIsDetectingGroupStatusChange();
							}
						};
					}
				}
				else
					lblGroupStatus.Text = string.Empty;
			}
		}
	}
}
