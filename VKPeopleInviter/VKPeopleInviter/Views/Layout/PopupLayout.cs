using System;
using Xamarin.Forms;

namespace VKPeopleInviter.Controls
{
	public class PopupLayout : ContentView
	{
		RelativeLayout rLayout;
		View popup;
		View content;

		public PopupLayout()
		{
			base.Content = rLayout = new RelativeLayout();

		}

		public new View Content
		{
			get
			{
				return content;
			}
			set
			{
				if (content != null)
				{
					rLayout.Children.Remove(content);
				}

				content = value;
				rLayout.Children.Add(value, () => Bounds);
			}
		}

		/// <summary>
		/// The is popup active.
		/// </summary>
		public bool isPopupActive
		{
			get
			{
				return popup != null;
			}
		}

		public void ShowPopup(View popupView)
		{
			this.ShowPopup(
				popupView,
				Constraint.RelativeToParent(p => (this.Width - popupView.WidthRequest) / 2),
				Constraint.RelativeToParent(p => (this.Height - popupView.HeightRequest) / 2)
				);

		}

		public void ShowPopupFromTop(View popupView)
		{
			this.ShowPopup(popupView,
			               Constraint.RelativeToParent((arg) => (this.X)),
			               Constraint.RelativeToParent((arg) => (this.Y)));
		}

		private void ShowPopup(View popupView, Constraint xConstraint, Constraint yConstraint, Constraint widthConstraint = null, Constraint heightConstraint = null)
		{
			DismissPopup();
			this.popup = popupView;

			this.rLayout.InputTransparent = true;
			this.content.InputTransparent = true;
			this.rLayout.Children.Add(this.popup, xConstraint, yConstraint, widthConstraint, heightConstraint);

			this.rLayout.ForceLayout();
		}

		public void DismissPopup()
		{
			if (this.popup != null)
			{
				this.rLayout.Children.Remove(popup);
				this.popup = null;
			}

			rLayout.InputTransparent = false;

			if (content != null)
			{
				content.InputTransparent = false;
			}
		}

		public View LatestChildren
		{
			get
			{
				if (rLayout.Children.Count == 0)
					return null;
				return rLayout.Children[rLayout.Children.Count - 1];
			}
		}


	}
}
