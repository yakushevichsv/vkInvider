using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace VKPeopleInviter
{
	public sealed partial class VKManager
	{
		private Dictionary<string, CancellationTokenSource> cacheMap = new Dictionary<string, CancellationTokenSource>();

		private static VKManager s_sharedInstance = new VKManager();

		static public VKManager sharedInstance()
		{
			return s_sharedInstance;
		}

		VKManager() { }


		public CancellationTokenSource CancelOperation(string key, bool cancel = true)
		{
			if (cacheMap.ContainsKey(key))
			{
				var cancelSource = cacheMap[key];
				cacheMap.Remove(key);
				if (cancel)
					cancelSource.Cancel();
				return cancelSource;
			}
			return null;
		}

		#region VK Methods' keys

		static string BasicAPIURL
		{
			get { return "https://api.vk.com/method/"; }
		}

		static string FormatVKMethodKey(string entityToChange, string methodName)
		{
			Debug.Assert(!string.IsNullOrEmpty(entityToChange) && !string.IsNullOrEmpty(methodName));
			return FormatVKMethodKey(entityToChange + "." + methodName);
		}


		static string FormatVKMethodKey(string jointMethodName)
		{
			Debug.Assert(jointMethodName.Contains("."));
			return BasicAPIURL + jointMethodName + "?";
		}

		string UsersSearchAPIKey
		{
			get { return FormatVKMethodKey("users", "search"); }
		}

		string CityInfoAPIKey
		{
			get { return FormatVKMethodKey("database", "getCities"); }
		}

		string FriendshipStatusAPIKey
		{
			get { return FormatVKMethodKey("friends", "areFriends"); }
		}

		string GroupsMembersKey
		{
			get { return FormatVKMethodKey("groups", "getMembers"); }
		}

		string AddOrCreateFriendRequestAPIKey
		{
			get { return FormatVKMethodKey("friends", "add"); }
		}

		#endregion

		public async Task<List<City>> ReceiveCities(string query, int country_id, int region_id, int count = 1)
		{
			string token = App.User.Token;

			string parameters = "q=" + query + "&region=" + region_id + "&country=" + country_id + "&offset=" + 0 + "&count=" + count + "&need_all=" + 1;

			string templatePart = CityInfoAPIKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelOperation(CityInfoAPIKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[CityInfoAPIKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				var retTokenSource = CancelOperation(CityInfoAPIKey);
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

		string FriendshipKey(string[] userIDs)
		{
			if (userIDs.Length == 0)
				return null;

			string resultIDs = "";

			foreach (string tempId in userIDs)
			{
				if (resultIDs.Length != 0)
					resultIDs = string.Concat(resultIDs, ",");
				resultIDs = string.Concat(resultIDs, tempId);
			}

			string parameters = "user_ids=" + resultIDs;
			string templateKey = FriendshipStatusAPIKey + parameters;

			return templateKey;
		}

		public bool CancelFriendshipDetection(string[] userIDs)
		{
			string templateKey = FriendshipKey(userIDs);

			if (templateKey == null)
				return false;

			return CancelOperation(templateKey) != null;
		}

		public async Task<Dictionary<string, FriendshipStatus>> DetectFriendshipStatusWithUsers(string[] userIDs)
		{
			string templateKey = FriendshipKey(userIDs);

			if (templateKey == null)
				return null;

			String token = App.User.Token;

			string template = templateKey + "&access_token=" + token;

			CancelOperation(templateKey);

			using (var client = new HttpClient())
			{
				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[templateKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				CancelOperation(templateKey);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();
					var dicIDs = new Dictionary<string, FriendshipStatus>();

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

											string fUserId = null;
											var fStatus = FriendshipStatus.NotAFriend;

											if (eObj.TryGetValue("uid", out tempToken)) //uid
												fUserId = tempToken.Value<string>();
											else {
												Debug.Assert(userIDs.Length == 1, "Not a one element in array");
												fUserId = userIDs.Last();
											}

											if (!(eObj.TryGetValue("friend_status", out tempToken) && Enum.TryParse(tempToken.Value<string>(), out fStatus)))
												continue;

											dicIDs[fUserId] = fStatus;
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
									throw new VKOperationException(errorCode, errorMsg);
								}
							}
						}
					}
					return dicIDs;
				}
			}
			return null;
		}


		//5835 - Rechica
		// 282 - Minsk
		public async Task<List<User>> SearchPeople(string query, int cityCode, int offset = 0, int count = 100)
		{
			//String template = "https://api.vk.com/method/METHOD_NAME?PARAMETERS&access_token=ACCESS_TOKEN"
			string token = App.User.Token;

			string parameters = "q=" + query + "&sort=1&fields=photo_100,uid,first_name,last_name,can_write_private_message" + "&sex=1&age_from=19&age_to=34" + "&country=3&city=" + cityCode + "&offset=" + offset + "&count=" + count;
			string templatePart = UsersSearchAPIKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelOperation(UsersSearchAPIKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[UsersSearchAPIKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					CancelOperation(UsersSearchAPIKey, false);

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
				CancelOperation(UsersSearchAPIKey, false);
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

			String token = App.User.Token;
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
									throw new VKOperationException(errorCode, errorMsg);
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

		string groupMemberAPIKey
		{
			get { return "https://api.vk.com/method/groups.isMember?"; }
		}

		string groupInviteAPIKey
		{
			get { return "https://api.vk.com/method/groups.invite?"; }
		}

		public bool CancelIsAGroupMemberDetection(string[] userIDs, string groupId)
		{
			if (userIDs.Length == 0)
				return false;

			var query = GetGroupDetectionTemplate(userIDs, groupId);

			return !((query == null) || CancelOperation(query) == null);
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

			string userIDName = userIDs.Length == 1 ? "user_id" : "user_ids";

			string parameters = "group_id=" + groupId + "&" + userIDName + "=" + resultIDs + "&extended=1";
			string templatekey = groupMemberAPIKey + parameters;

			return templatekey;
		}

		private string GetAddOrCreateFriendRequestTemplate(string userId)
		{
			if (String.IsNullOrEmpty(userId)) return null;

			string parameters = "user_id=" + userId;
			string templateKey = AddOrCreateFriendRequestAPIKey + parameters;
			return templateKey;
		}

		public async Task<int> InviteUserToAGroup(string userId, string groupId)
		{
			string token = App.User.Token;

			string parameters = "group_id=" + groupId + "&user_id=" + userId;
			var groupInviteTemplateKey = groupInviteAPIKey + parameters;
			var groupInvite = groupInviteTemplateKey + "&access_token=" + token;
			CancelOperation(groupInviteTemplateKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[groupInviteTemplateKey] = tokenSource;

				var response = await client.GetAsync(groupInvite, tokenSource.Token).ConfigureAwait(false);

				CancelOperation(groupInviteTemplateKey);
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
									throw new VKOperationException(errorCode, errorMsg);
								}
							}
						}
					}
					return 0;
				}
			}
			return 0;
		}

		public async Task<FriendAddStatus> CreateOrApproveFriendRequest(string userId, string text = null, int? follow = null)
		{
			Debug.Assert(!string.IsNullOrEmpty(userId));
			var templateKey = GetAddOrCreateFriendRequestTemplate(userId);
			if (templateKey == null) return FriendAddStatus.FriendRequestNone;

			string token = App.User.Token;
			string template = templateKey;

			CancelOperation(templateKey);

			if (!string.IsNullOrEmpty(text))
				template += "&text=" + text;

			if (follow.HasValue)
				template += "&follow=" + follow.Value;

			template += "&access_token=" + token;


			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[templateKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				CancelOperation(templateKey);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();
					var retStatus = FriendAddStatus.FriendRequestNone;

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
									if (Enum.TryParse(value.ToString(), out retStatus))
										return retStatus;
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
									throw new VKOperationException(errorCode, errorMsg);
								}
							}
						}
					}
				}
				return FriendAddStatus.FriendRequestNone;
			}
		}

		public async Task<UserGroupStatus[]> DetectIfUserIsAGroupMember(string[] userIDs, string groupId)
		{

			string token = App.User.Token;
			var templateKey = GetGroupDetectionTemplate(userIDs, groupId);
			if (templateKey == null)
				return null;

			string template = templateKey + "&access_token=" + token;


			CancelOperation(templateKey);

			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[templateKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);

				CancelOperation(templateKey);
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
												if (eObj.TryGetValue("request", out tempToken) && tempToken.Value<bool>())
												{
													status = UserGroupStatus.Requested;
												}
												else if (eObj.TryGetValue("invitation", out tempToken) && tempToken.Value<bool>())
												{
													status = UserGroupStatus.Invited;
												}

											}


											statuses.Add(status);
										}
									}
								}
								else if (value.Type == JTokenType.Object)
								{
									JToken tempToken;
									var eObj = (JObject)value;
									UserGroupStatus status = UserGroupStatus.CanBeInvited;

									if (eObj.TryGetValue("member", out tempToken) && tempToken.Value<bool>())
										status = UserGroupStatus.Member;
									else
									{
										if (eObj.TryGetValue("request", out tempToken) && tempToken.Value<bool>())
										{
											status = UserGroupStatus.Requested;
										}
										else if (eObj.TryGetValue("invitation", out tempToken) && tempToken.Value<bool>())
										{
											status = UserGroupStatus.Invited;
										}

									}


									statuses.Add(status);
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
									throw new VKOperationException(errorCode, errorMsg);
								}
							}
						}
					}
					return statuses.ToArray();
				}
			}
			return null;
		}

		#region Groups Methods




		/// <summary>
		/// Method for receiving information about group members.
		/// </summary>
		/// <returns>Returns count & users ids, or User object if fields where provided.</returns>
		/// <param name="groupId">Id of the group</param>
		/// <param name="offset">Offset from the beggining to search</param>
		/// <param name="count">Number of returned items, 1000 by default</param>
		/// <param name="sort">If set to <c>true</c> sort. in ascending.</param>
		/// <param name="fields">Extra fields to return <list type="string"> <item>can_write_private_message</item> </list> </param>
		public async Task<TotalListOfUsers> GroupsGetMembers(string groupId, int offset = 0, int count = 1000, bool sort = true, string[] fields = null)
		{
			string token = App.User.Token;

			List<string> eFields = null;

			if (fields == null || fields.Length == 0)
				eFields = new List<string>() { "first_name", "last_name", "photo_100", "uid", "can_write_private_message" };
			else
				eFields = new List<string>(fields);

			var resultFields = "";

			foreach (string tempId in eFields)
			{
				if (resultFields.Length != 0)
					resultFields = string.Concat(tempId, ",");
				resultFields = string.Concat(resultFields, tempId);
			}

			string parameters = "group_id=" + groupId + "&sort=" + (sort == true ? "id_asc" : "id_desc")  +"&fields=" + resultFields +  "&offset=" + offset + "&count=" + count;
			string templatePart = GroupsMembersKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelOperation(GroupsMembersKey);

			using (var client = new HttpClient())
			{
				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[GroupsMembersKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					CancelOperation(GroupsMembersKey, false);

					var totalList = JsonConvert.DeserializeObject<TotalListOfUsersWrapper>(jsonString);



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


					index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index + 1);

					index = jsonString.IndexOf('{');
					if (index == -1)
					{
						//TODO: analyze response and keep total amonunt of found users.
						throw new UsersNotFoundException("Users were not found!");
					//return new List<User>();
					}
					jsonString = jsonString.Substring(index);
					var finalString = "{response:[" + jsonString;
					finalString = finalString.Substring(0, finalString.Length - 1);
					//TODO: why it doesn't work?
					var responseUsers = JsonConvert.DeserializeObject<ResponseUsers>(finalString);

					totalList.totalListOfUsers.users =  responseUsers.users;
				}
				CancelOperation(GroupsMembersKey, false);
			}
			return new TotalListOfUsers();
		}

		#endregion
	}

	#region Public Enum

	public partial class VKManager
	{
		public enum FriendshipStatus
		{
			NotAFriend,
			SendFriendRequest,
			ReceivedFriendRequest,
			MutualFriend
		};

		public enum UserGroupStatus
		{
			CanBeInvited,
			Detecting,
			Member,
			Invited,
			Requested,
			Failed,
			Cancelled
		};

		public enum FriendAddStatus
		{
			FriendRequestNone = VKManager.None,
			FriendRequestSent = 1,
			FriendRequestFromUserApproved = 2,
			FriendRequestResending = 4
		};

		public const int None = 0;
	}

	public static class VKManageExtension
	{
		public static string ToString(this VKManager.FriendshipStatus status)
		{
			string result = null;
			switch (status)
			{
				case VKManager.FriendshipStatus.NotAFriend:
					result = "Not a Friend";
					break;
				case VKManager.FriendshipStatus.SendFriendRequest:
					result = "Friend request was sent";
					break;
				case VKManager.FriendshipStatus.ReceivedFriendRequest:
					result = "Received friend request";
					break;
				default:
					Debug.Assert(status == VKManager.FriendshipStatus.MutualFriend);
					result = "Your friend";
					break;
			}
			return result;
		}

		public static string ToString(this VKManager.UserGroupStatus status)
		{
			string result = null;
			switch (status)
			{
				case VKManager.UserGroupStatus.CanBeInvited:
					result = "Can be invited";
					break;
				case VKManager.UserGroupStatus.Cancelled:
					result = "Cancelled";
					break;
				case VKManager.UserGroupStatus.Detecting:
					result = "Detecting";
					break;
				case VKManager.UserGroupStatus.Failed:
					result = "Failed";
					break;
				case VKManager.UserGroupStatus.Invited:
					result = "Invited";
					break;
				case VKManager.UserGroupStatus.Requested:
					result = "Requested";
					break;
				default:
					Debug.Assert(status == VKManager.UserGroupStatus.Member);
					result = "Member";
					break;
			}
			return result;
		}
	}

	#endregion

	public sealed class UsersNotFoundException : Exception
	{
		public UsersNotFoundException(string message) : base(message) { }
	}

	public sealed partial class VKOperationException : Exception
	{
		public int ErrorCode { get; private set; }

		public VKOperationException(int code, string message) : base(message) { ErrorCode = code; }

		public ErrorCodeStatus ErrorStatusCode
		{
			get
			{
				if (Enum.IsDefined(typeof(ErrorCodeStatus), ErrorCode))
					return (ErrorCodeStatus)ErrorCode;
				return ErrorCodeStatus.None;
			}
		}
	}

	partial class VKOperationException
	{
		public enum ErrorCodeStatus
		{
			None = VKManager.None,
			TooManyRequests = 6, //Too many requests per second
			AccessDenied = 15
		};
	}
}