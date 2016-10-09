using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernDev;
using ModernDev.InTouch;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace VKPeopleInviter
{
	public sealed class VKManager
	{
		private int clientId = 5537512;
		private string clientSecretToken = "E9x6ywxHcYnnqf3ZXtjd";

		private Dictionary<string, CancellationTokenSource> cacheMap = new Dictionary<string, CancellationTokenSource>();

		private static VKManager s_sharedInstance = new VKManager();

		static public VKManager sharedInstance()
		{
			return s_sharedInstance;
		}

		private InTouch client { get; set; }
		private VKManager()
		{
			client = new InTouch(true, true);//new InTouch(false, true);
			client.SetApplicationSettings(clientId, clientSecretToken);
		}

		private void Client_CaptchaNeeded(object sender, ResponseError e)
		{
			Debug.WriteLine(e.Message);
		}

		private void Client_AuthorizationFailed(object sender, ResponseError e)
		{
			Debug.WriteLine(e.Message);
		}

		public void didAuthorizeWithToken(String token, int userId, int duration)
		{
			if (duration != 0)
			{
				client.SetSessionData(token, userId, duration);
			}
			else {
				client.SetSessionData(token, userId);
			}

		}

		public CancellationTokenSource CancelSearchPeople(string query, bool cancel = true)
		{
			if (cacheMap.ContainsKey(query))
			{
				var cancelSource = cacheMap[query];
				cacheMap.Remove(query);
				if (cancel)
					cancelSource.Cancel();
				return cancelSource;
			}
			return null;
		}

		string cancelSearchAPIKey
		{
			get { return "https://api.vk.com/method/users.search?"; }
		}

		string cityInfoAPIKey
		{
			get { return "https://api.vk.com/method/database.getCities?"; }
		}

		public async Task<List<City>> ReceiveCities(string query, int country_id, int region_id, int count = 1)
		{
			string token = App.User.Token;

			string parameters = "q=" + query + "&region=" + region_id + "&country=" + country_id + "&offset=" + 0 + "&count=" + count + "&need_all=" + 1;

			string templatePart = cityInfoAPIKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelSearchPeople(cityInfoAPIKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[cityInfoAPIKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				var retTokenSource = CancelSearchPeople(cityInfoAPIKey);
				if (response.IsSuccessStatusCode && !(retTokenSource == null || retTokenSource.IsCancellationRequested))
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index + 1);

					index = jsonString.IndexOf('{');
					if (index == -1)
					{
						return new List<City>();
					}
					jsonString = jsonString.Substring(index);
					var length = jsonString.Length;

					var finalString = "{response:[" + jsonString;
					var responseUsers = JsonConvert.DeserializeObject<ResponseCities>(finalString);
					return new List<City>(responseUsers.Response.Items);
				}
			}
			return new List<City>();
		}


		//5835 - Rechica
		// 282 - Minsk
		public async Task<List<User>> SearchPeople(string query, int cityCode, int offset = 0, int count = 100)
		{
			//String template = "https://api.vk.com/method/METHOD_NAME?PARAMETERS&access_token=ACCESS_TOKEN"
			string token = App.User.Token;

			string parameters = "q=" + query + "&sort=1&fields=photo_100,uid,first_name,last_name" + "&sex=1&age_from=19&age_to=34" + "&country=3&city=" + cityCode + "&offset=" + offset + "&count=" + count;
			string templatePart = cancelSearchAPIKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelSearchPeople(cancelSearchAPIKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[cancelSearchAPIKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					CancelSearchPeople(cancelSearchAPIKey, false);

					var index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index + 1);

					index = jsonString.IndexOf('{');
					if (index == -1)
					{
						//TODO: analyze response and keep total amonunt of found users.
						throw new UsersNotFoundException("Users were not found!");
						//return new List<User>();
					}
					jsonString = jsonString.Substring(index);
					var length = jsonString.Length;

					var finalString = "{response:[" + jsonString;
					var responseUsers = JsonConvert.DeserializeObject<ResponseUsers>(finalString);
					return new List<User>(responseUsers.users);
				}
				CancelSearchPeople(cancelSearchAPIKey, false);
			}
			return new List<User>();
		}

		public async Task<long[]> SendMessageToUsers(string message, string[] userIDs)
		{
			if (userIDs.Length == 0 || message.Length == 0)
			{
				return null;
			}

			string resultIDs = "";

			foreach (string userId in userIDs)
			{
				if (resultIDs.Length != 0)
					resultIDs = String.Concat(resultIDs, ",");
				resultIDs = String.Concat(resultIDs, userId);
			}

			String token = this.client.Session.AccessToken;
			string parameters = "user_ids=" + resultIDs + "&message=" + WebUtility.UrlEncode(message);

			String template = "https://api.vk.com/method/messages.send?" + parameters + "&access_token=" + token;
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(template).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;
					Debug.WriteLine("Content " + content);

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();
					long[] ids = new long[0];

					foreach (JToken jToken in resultObj)
					{
						if (jToken.Type == JTokenType.Property)
						{
							var property = (JProperty)jToken;
							if (property.Name == "response")
							{
								var value = property.Value;
								if (value.Type == JTokenType.Array)
								{
									var array = (JArray)value;
									ids = array.Select(arg1 => (long)arg1).ToArray();
								}
							}
							else if (property.Name == "error")
							{
								var value = property.Value;

								if (value.Type == JTokenType.Object)
								{
									var obj = (JObject)value;

									var errorMsg = obj["error_msg"].Value<string>();
									var errorCode = obj["error_code"].Value<int>();
									Debug.WriteLine("Error executing operation: Code " + errorCode + "Message " + errorMsg);

#if DEBUG
									errorMsg += "Code " + errorCode;
#endif
									throw new VKOperationException(errorMsg);
								}
							}
						}
					}
					return ids;
				}
			}
			return null;
		}

		//MARK: Group methods..

		public enum UserGroupStatus  {  None, Detecting,  Member, Invited, Requested, Failed, Cancelled, CanBeInvited };


		string groupMemberAPIKey
		{
			get { return "https://api.vk.com/method/groups.isMember?"; }
		}

		string groupInviteAPIKey
		{
			get {return "https://api.vk.com/method/groups.invite?"; }
		}

		public bool CancelIsAGroupMemberDetection(string[] userIDs, string groupId)
		{
			if (userIDs.Length == 0)
				return false;

			var query = GetGroupDetectionTemplate(userIDs, groupId);

			return !((query == null) || CancelSearchPeople(query) == null);
		}

		private string GetGroupDetectionTemplate(string[] userIDs, string groupId)
		{

			if (userIDs.Length == 0)
				return null;

			string resultIDs = "";

			foreach (string userId in userIDs)
			{
				if (resultIDs.Length != 0)
					resultIDs = String.Concat(resultIDs, ",");
				resultIDs = String.Concat(resultIDs, userId);
			}

			string parameters = "group_id=" + groupId + "&user_ids=" + resultIDs + "&extended=1";
			string templatekey = groupMemberAPIKey + parameters;

			return templatekey;
		}

		public async Task<int> InviteUserToAGroup(string userId, string groupId)
		{
			string token = App.User.Token;

			string parameters = "group_id=" + groupId + "&user_id=" + userId;
			var groupInviteTemplateKey = groupInviteAPIKey + parameters;
			var groupInvite = groupInviteTemplateKey + "&access_token=" + token;
			CancelSearchPeople(groupInviteTemplateKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[groupInviteTemplateKey] = tokenSource;

				var response = await client.GetAsync(groupInvite, tokenSource.Token).ConfigureAwait(false);

				CancelSearchPeople(groupInviteTemplateKey);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;
					Debug.WriteLine("Content " + content);

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();
					var statuses = new List<UserGroupStatus>();

					foreach (JToken jToken in resultObj)
					{
						if (jToken.Type == JTokenType.Property)
						{
							var property = (JProperty)jToken;
							if (property.Name == "response")
							{
								var value = property.Value;
								if (value.Type == JTokenType.Integer)
								{
									var obj = (JObject)value;

									return obj.Value<int>();
								}
								return 0;
							}
							else if (property.Name == "error")
							{
								var value = property.Value;

								if (value.Type == JTokenType.Object)
								{
									var obj = (JObject)value;

									var errorMsg = obj["error_msg"].Value<string>();
									var errorCode = obj["error_code"].Value<int>();
									Debug.WriteLine("Error executing operation: Code " + errorCode + "Message " + errorMsg);

#if DEBUG
									errorMsg += " Code " + errorCode;
#endif
									throw new VKOperationException(errorMsg);
								}
							}
						}
					}
					return 0;
				}
			}
			return 0;
		}

		public async Task<UserGroupStatus[]> DetectIfUserIsAGroupMember(string[] userIDs, string groupId)
		{

			string token = App.User.Token;
			var templateKey = GetGroupDetectionTemplate(userIDs, groupId);
			if (templateKey == null)
				return null;

			string template = templateKey + "&access_token=" + token;


			CancelSearchPeople(templateKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[templateKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);

				CancelSearchPeople(templateKey);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;
					Debug.WriteLine("Content " + content);

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();
					var statuses = new List<UserGroupStatus>();

					foreach (JToken jToken in resultObj)
					{
						if (jToken.Type == JTokenType.Property)
						{
							var property = (JProperty)jToken;
							if (property.Name == "response")
							{
								var value = property.Value;
								if (value.Type == JTokenType.Array)
								{
									var array = (JArray)value;

									foreach (var element in array)
									{
										var eObj = (JObject)element;

										if (eObj.Type == JTokenType.Object)
										{
											JToken tempToken;

											UserGroupStatus status = UserGroupStatus.CanBeInvited;

											if (eObj.TryGetValue("member", out tempToken) && tempToken.Value<bool>())
												status = UserGroupStatus.Member;
											else
											{
												if (eObj.TryGetValue("request", out tempToken) && tempToken.Value<bool>()) {
													status = UserGroupStatus.Requested;
												}
												else if (eObj.TryGetValue("invitation", out tempToken) && tempToken.Value<bool>()) {
													status = UserGroupStatus.Invited;
												}

											}


											statuses.Add(status);
										}
									}
								}
							}
							else if (property.Name == "error")
							{
								var value = property.Value;

								if (value.Type == JTokenType.Object)
								{
									var obj = (JObject)value;

									var errorMsg = obj["error_msg"].Value<string>();
									var errorCode = obj["error_code"].Value<int>();
									Debug.WriteLine("Error executing operation: Code " + errorCode + "Message " + errorMsg);

#if DEBUG
									errorMsg += " Code " + errorCode;
#endif
									throw new VKOperationException(errorMsg);
								}
							}
						}
					}
					return statuses.ToArray();
				}
			}
			return null;
		}
	}

	sealed class UsersNotFoundException : Exception
	{
		public UsersNotFoundException(string message) : base(message) { }
	}

	sealed class VKOperationException : Exception
	{
		public VKOperationException(string message) : base(message) { }
	}
}