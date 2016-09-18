using System;
using System.ComponentModel;

namespace VKPeopleInviter
{
	public class MultipleItemSelectlon<T> : INotifyPropertyChanged
	{
		private bool m_Selected;

		public event PropertyChangedEventHandler PropertyChanged;

		public Boolean Selected
		{
			get
			{
				return m_Selected;
			}
			set
			{
				m_Selected = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
			}
		}

		public T Item { get; set;}
	}

}

