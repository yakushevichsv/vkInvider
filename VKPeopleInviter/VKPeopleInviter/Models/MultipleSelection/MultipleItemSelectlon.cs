using System;
using System.ComponentModel;

namespace VKPeopleInviter
{
	//TODO: Create base class which is not selectable...

	public class MultipleItemSelectlon<T> : INotifyPropertyChanged
	{
		bool m_Selected;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool Selected
		{
			get
			{
				return m_Selected;
			}
			set
			{
				m_Selected = value;
				NotifyAboutPropertyChange("Selected");
			}
		}

		public T Item { get; set; }

		protected void NotifyAboutPropertyChange(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}

	public class GroupDetectionMultipleItemSelection<T> : MultipleItemSelectlon<T>
	{
		VKManager.UserGroupStatus m_Status = VKManager.UserGroupStatus.Cancelled;

		public VKManager.UserGroupStatus Status
		{
			get
			{
				return m_Status;
			}
			set
			{
				m_Status = value;
				NotifyAboutPropertyChange("Status");
			}
		}

		public bool isDetectionNeeded
		{
			get
			{
				return !isDetecting && (isFailed || isCancelled);
			}
		}

		public bool isCancelled
		{
			get
			{
				return m_Status == VKManager.UserGroupStatus.Cancelled;
			}
		}

		public bool isDetecting
		{
			get
			{
				return m_Status == VKManager.UserGroupStatus.Detecting;
			}
		}


		public bool isFailed
		{
			get
			{
				return m_Status == VKManager.UserGroupStatus.Failed; 
			}
		}
	}
}

