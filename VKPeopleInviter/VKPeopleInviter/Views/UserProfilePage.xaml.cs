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
				Title = userProfile.user.FirstName ?? userProfile.user.LastName;
				Debug.WriteLine("Getting current user"); 
				var users = await vkManager.GetUsers(new long[] { userProfile.user.Id });
				this.serverUser = users.Items[users.Items.Length - 1];

				//TODO: refactor this using MVVM, bindings....
				var imageUri = this.serverUser.ImageUri;
				if (imageUri != null)
				{
					imagePicture.Source = new UriImageSource {Uri = new Uri(imageUri) };
					await Task.WhenAll(imagePicture.TranslateTo(0, 20, 150, Easing.Linear),
										   lblFullName.TranslateTo(0, 20, 150, Easing.Linear));
						
				}
				else
					this.imagePicture.IsVisible = false;
				this.lblFullName.Text = userProfile.user.FullName ?? this.serverUser.FullName;  
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
					DelayActionAsync(4, StopActivityIndicator);
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


		void DelayActionAsync(int delayInSeconds, Action action)
		{
			var t =  Task.Delay(delayInSeconds * 1000);
			Task.WaitAll(new Task[] { t });
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
