using System;
namespace VKPeopleInviter
{
	public class UserProfile
	{
		internal User user { private set; get; }

		public UserProfile(User user)
		{
			this.user = user;
		}
	}
}
