using System;
using System.ComponentModel;

namespace VKPeopleInviter
{
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
		VKManager.UserGroupStatus m_Status = VKManager.UserGroupStatus.None;

		bool m_isDetectingStatus;

		public bool isDetectingStatus
		{
			get
			{
				return m_isDetectingStatus;
			}
			set
			{
				m_isDetectingStatus = value;
				NotifyAboutPropertyChange("DetectingStatus");
			}
		}

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
	}
}

