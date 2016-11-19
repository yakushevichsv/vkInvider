
using System;
using System.Net;

public static class ArrayOfStringsExtension
{
	public static string FirstNonNullAndNonEmptyString(this string[] items)
	{
		if (items == null || items.Length == 0)
			return null;

		foreach (var item in items)
			if (!string.IsNullOrEmpty(item))
				return item;

		return null;
	}

	public static string CommaJointItems(this string[] items)
	{
		string result = "";

		foreach (var item in items)
		{
			if (result.Length != 0)
				result = string.Concat(result, ",");
			result = string.Concat(result, item);
		}
		return result;
	}
}

public static class ArrayOfLongExtension
{
	public static string CommaJointItems(this long[] items)
	{
		string result = "";

		foreach (var item in items)
		{
			if (result.Length != 0)
				result = string.Concat(result, ",");
			result = string.Concat(result, item);
		}
		return result;
	}
}

public static class ExceptionIsNetwork
{
	public static bool IsNetworkException(this Exception exception)
	{
		var webException = exception as System.Net.WebException;

		var status = webException.Status.ToString();

		if (webException != null)
		{
			//TODO: figure out why there is no status....
			if (WebExceptionStatus.ConnectFailure == webException.Status || WebExceptionStatus.SendFailure == webException.Status || status == "NameResolutionFailure")
				return true;

		}

		return false;
	}
}