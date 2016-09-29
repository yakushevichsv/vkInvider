using System;

using Xamarin.Forms;

public enum FriendShipStatus
{
	None,
	Following,
	Followed,
	Blocked, // he is blocked
	Blocking,
	Friend
};

namespace VKPeopleInviter.Controls
{
	public class UserSelectableCell : ViewCell
	{
		public static readonly BindableProperty FullNameProperty =
			BindableProperty.Create("FullName", typeof(String), typeof(UserSelectableCell), String.Empty);

		public static readonly BindableProperty ImageSourceProperty =
			BindableProperty.Create("ImageSource", typeof(ImageSource), typeof(UserSelectableCell), null);

		public static readonly BindableProperty FriendshipStatusProperty =
			BindableProperty.Create("FriendShipStatus", typeof(FriendShipStatus), typeof(UserSelectableCell), FriendShipStatus.None);

		public static readonly BindableProperty SelectedProperty =
			BindableProperty.Create("Selected", typeof(bool), typeof(UserSelectableCell), false);

		public string FullName
		{
			get
			{
				return (string)GetValue(FullNameProperty);
			}
			set
			{
				SetValue(FullNameProperty, value);
			}
		}

		public ImageSource ImageSource
		{
			get
			{
				return (ImageSource)GetValue(ImageSourceProperty);
			}
			set
			{
				SetValue(ImageSourceProperty, value);
			}
		}

		public FriendShipStatus FriendShip
		{
			get
			{
				return ConvertToFriendShipFromObject(GetValue(FriendshipStatusProperty));
			}
			set
			{
				SetValue(FriendshipStatusProperty, value.ToString());
			}
		}

		Label lblFullName, lblFriendShip;
		Image ivPicture, ivSelected;

		static ImageSource sCheckedImageSource = ImageSource.FromResource("VKPeopleInviter.Resources.checked.png");
		static ImageSource sUnckeckedImageSource = ImageSource.FromResource("VKPeopleInviter.Resources.checkbox.png");

		public bool Selected
		{
			get
			{
				return Convert.ToBoolean(GetValue(SelectedProperty).ToString());
			}
			set
			{
				SetValue(SelectedProperty, value.ToString());

				ivSelected.Source = value ? sCheckedImageSource : sUnckeckedImageSource;
			}
		}

		public UserSelectableCell()
		{
			lblFullName = new Label() { HorizontalOptions = LayoutOptions.StartAndExpand };
			ivPicture = new Image() { Aspect = Aspect.AspectFit };
			ivPicture.HeightRequest = 32;
			lblFriendShip = new Label() { HorizontalOptions = LayoutOptions.Start};
			ivSelected = new Image() { Aspect = Aspect.AspectFit };
			ivSelected.HeightRequest = 16;

			var aiIndicator = new ActivityIndicator();
			aiIndicator.IsRunning = false;

			this.SetBinding(SelectedProperty,"Selected", BindingMode.TwoWay);

			StackLayout cellWrapper = new StackLayout();
			StackLayout contentLayout = new StackLayout();
			contentLayout.Orientation = StackOrientation.Horizontal;

			contentLayout.Children.Add(lblFullName);
			contentLayout.Children.Add(ivPicture);
			contentLayout.Children.Add(lblFriendShip);
			contentLayout.Children.Add(ivSelected);
			contentLayout.Children.Add(aiIndicator);
			cellWrapper.Children.Add(contentLayout);

			cellWrapper.HeightRequest = ivPicture.HeightRequest + 2;

			this.View = cellWrapper;

			/*
			var layout = new Grid()
			{
				ColumnDefinitions = {
					new ColumnDefinition {Width = new GridLength(4,GridUnitType.Star)},
					new ColumnDefinition {Width = new GridLength(1,GridUnitType.Star)}
				},
				RowDefinitions = {
					new RowDefinition { Height = new GridLength(1,GridUnitType.Star)},
					new RowDefinition { Height = new GridLength(3,GridUnitType.Star)},
					new RowDefinition { Height = new GridLength(1,GridUnitType.Star)},
				}
			};
			layout.Children.Add(lblFullName, 0, 0);
			layout.Children.Add(ivPicture, 0, 1);
			layout.Children.Add(lblFriendShip, 0, 2);
			layout.Children.Add(ivSelected, 1, 0);
			Grid.SetRowSpan(ivSelected, 3);

			this.View = layout; */


		}

		static FriendShipStatus ConvertToFriendShipFromObject(object ojb)
		{
			var result = FriendShipStatus.None;

			if (Enum.TryParse(ojb.ToString(), out result))
			{
				if (Enum.IsDefined(typeof(FriendShipStatus), result) == false)
				{
					result = FriendShipStatus.None;
				}
			}

			return result;
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			if (BindingContext != null)
			{
				lblFullName.Text = FullName;
				ivPicture.Source = ImageSource;
				ivSelected.Source = Selected ? sCheckedImageSource : sUnckeckedImageSource;
			}
		}
	}
}


