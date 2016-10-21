using System;

namespace VKPeopleInviter
{
	public static class DateTimeUnix
	{
		public static int ToUnixTimestamp(this DateTime value)
		{
			return (int)Math.Truncate((value.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc))).TotalSeconds);
		}
	}
}

