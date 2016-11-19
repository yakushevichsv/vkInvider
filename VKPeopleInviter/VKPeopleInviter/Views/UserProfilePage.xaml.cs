using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	public partial class UserProfilePage : ContentPage
	{
		UserProfile _profile;
		VKManager vkManager = VKManager.sharedInstance();

		private User serverUser = null;

		public UserProfile userProfile
		{
			get { return _profile; }
			set
			{
				if (value != _profile)
				{
					_profile = value;
					Setup();
				}
			}
		}

		public UserProfilePage()
		{
			InitializeComponent();
		}


		#region Setup
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
		async void Setup()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
		{
			try
			{

				Debug.WriteLine("Getting current user"); 
				var users = await vkManager.GetUsers(new long[] { userProfile.user.Id });
				this.serverUser = users.Items[users.Items.Length - 1];
				Debug.WriteLine("Current user received!");
			}
			catch (Exception exp)
			{
				//TODO.. analyze internet absence...
				Debug.WriteLine("Exception " + exp);


				if (exp.IsNetworkException())
				{
					//containingLayout.ShowPopupFromTop( new 
					RunActivityIndicator("No Internet ");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
					DelayActionAsync(4, StopActivityIndicator);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				}
			}
		}

		#endregion

		#region Activity Indicator 

		private void RunActivityIndicator(string message)
		{
			var label = new Label()
			{
				HeightRequest = 100,
				WidthRequest = this.Width,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				TextColor = Color.White,
				BackgroundColor = Color.Black,
				Opacity = 0.7
			};
			label.Text = message;
			containingLayout.ShowPopupFromTop(label);
		}

		private void StopActivityIndicator()
		{
			containingLayout.DismissPopup();
		}

		#endregion


		private async Task DelayActionAsync(int delayInSeconds, Action action)
		{
			await Task.Delay(delayInSeconds * 1000);

			action();
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height); //must be called

			if (containingLayout.isPopupActive)
			{
				var label = containingLayout.LatestChildren as Label;
				if (label == null)
					return;

				label.WidthRequest = width;
			}
		}
	}
}
