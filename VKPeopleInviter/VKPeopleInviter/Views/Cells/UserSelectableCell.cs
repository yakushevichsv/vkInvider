using System;

using Xamarin.Forms;

public enum FriendShipStatus
{
	None,
	Following,
	Followed,
	Blocked, // he is blocked
	Blocking,
	Friend,
	Failed
};

namespace VKPeopleInviter.Controls
{
	public class UserSelectableCell : ViewCell
	{
		public static readonly BindableProperty FullNameProperty =
			BindableProperty.Create("FullName", typeof(string), typeof(UserSelectableCell), String.Empty, BindingMode.TwoWay);

		public static readonly BindableProperty ImageUriProperty =
			BindableProperty.Create("ImageUri", typeof(string), typeof(UserSelectableCell), null , BindingMode.TwoWay);

		public static readonly BindableProperty FriendshipStatusProperty =
			BindableProperty.Create("FriendShipStatus", typeof(FriendShipStatus), typeof(UserSelectableCell), FriendShipStatus.None , BindingMode.TwoWay);

		public static readonly BindableProperty SelectedProperty =
			BindableProperty.Create("Selected", typeof(bool), typeof(UserSelectableCell), false, BindingMode.TwoWay);

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

		public string ImageUri
		{
			get
			{
				return (string)GetValue(ImageUriProperty);
			}
			set
			{
				SetValue(ImageUriProperty, value);
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

		protected StackLayout contentLayout { set; get;}
		protected RelativeLayout relativeLayout { set; get;}

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

		public EventArgs e = null;
		public event ButtonClickEvent ClickListener;
		public delegate void ButtonClickEvent(UserSelectableCell m, EventArgs e);

		public UserSelectableCell() : this(true) { }

		public UserSelectableCell(bool displaySwitch) : base()
		{
			lblFullName = new Label() { HorizontalOptions = LayoutOptions.StartAndExpand };
			ivPicture = new Image() { Aspect = Aspect.AspectFit };
			ivPicture.HeightRequest = 32;
			lblFriendShip = new Label() { HorizontalOptions = LayoutOptions.Start};
			ivSelected = new Image() { Aspect = Aspect.AspectFit };
			ivSelected.HeightRequest = 16;


			this.SetBinding(SelectedProperty, new Binding("Selected"){ Mode = BindingMode.TwoWay });


			RelativeLayout cellWrapper = new RelativeLayout();
			contentLayout = new StackLayout();
			contentLayout.Orientation = StackOrientation.Horizontal;

			contentLayout.Children.Add(lblFullName);
			contentLayout.Children.Add(ivPicture);
			contentLayout.Children.Add(lblFriendShip);
			ivSelected.IsVisible = false;
			contentLayout.Children.Add(ivSelected);


			if (displaySwitch)
			{
				Switch mainSwitch = new Switch();
				mainSwitch.SetBinding(Switch.IsToggledProperty, new Binding("Selected") { Mode = BindingMode.TwoWay });

				mainSwitch.Toggled += (sender, e) =>
				{
					bool selected = e.Value;
					Selected = selected;
					if (ClickListener != null)
						ClickListener(this, e);
				};
				contentLayout.Children.Add(mainSwitch);
			}

			cellWrapper.Children.Add(contentLayout,
				Constraint.Constant(0),
			                         Constraint.Constant(0),
				Constraint.RelativeToParent((parent) =>
					{
						return parent.Width;
					}),
				Constraint.RelativeToParent((parent) =>
					{
				return parent.Height;
					})
			);

			cellWrapper.HeightRequest = ivPicture.HeightRequest + 2;

			this.View = cellWrapper;
			this.relativeLayout = cellWrapper;
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
				var pictureSource = ImageSource.FromUri(new Uri(ImageUri));
				ivSelected.Source = Selected ? sCheckedImageSource : sUnckeckedImageSource;
				ivPicture.Source = pictureSource;
				var obj = BindingContext as MultipleItemSelectlon<User>;
				if (obj != null)
				{
					obj.PropertyChanged += (sender, e) =>
					{
						var senderObj = sender as MultipleItemSelectlon<User>;
						if (e.PropertyName == "Selected")
						{
							Selected = senderObj.Selected;
						}
					};
				}
			}
		}
	}
}


