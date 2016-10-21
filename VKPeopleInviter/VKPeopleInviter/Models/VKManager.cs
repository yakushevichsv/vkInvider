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

		string MessagesSearchKey
		{
			get { return FormatVKMethodKey("messages", "search"); }
		}

		static string FormatStringFromDic(Dictionary<string, object> obj, HashSet<string> keyForEncoding)
		{
			var builder = new StringBuilder();
			foreach (var keyPair in obj)
			{
				var value = keyForEncoding.Contains(keyPair.Key) ? WebUtility.UrlEncode(keyPair.Value.ToString()) : keyPair.Value;
				if (builder.Length != 0)
					builder.Append("&");

				builder.Append(keyPair.Key + "=" + value);
			}
			return builder.ToString();
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

		string FriendshipKey(long[] userIDs)
		{
			string resultIDs = "";

			foreach (var tempId in userIDs)
			{
				if (resultIDs.Length != 0)
					resultIDs = string.Concat(resultIDs, ",");
				resultIDs = string.Concat(resultIDs, tempId);
			}

			string parameters = "user_ids=" + resultIDs;
			string templateKey = FriendshipStatusAPIKey + parameters;

			return templateKey;
		}

		public bool CancelFriendshipDetection(long[] userIDs)
		{
			string templateKey = FriendshipKey(userIDs);

			if (templateKey == null)
				return false;

			return CancelOperation(templateKey) != null;
		}

		public async Task<Dictionary<string, FriendshipStatus>> DetectFriendshipStatusWithUsers(long[] userIDs)
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

											long? fUserId = null;
											var fStatus = FriendshipStatus.NotAFriend;

											if (eObj.TryGetValue("uid", out tempToken)) //uid
												fUserId = tempToken.Value<long>();
											else {
												Debug.Assert(userIDs.Length == 1, "Not a one element in array");
												fUserId = userIDs.Last();
											}

											if (!(eObj.TryGetValue("friend_status", out tempToken) && Enum.TryParse(tempToken.Value<string>(), out fStatus)))
												continue;

											if (fUserId.HasValue)
												dicIDs[fUserId.ToString()] = fStatus;
										}
									}
								}
							}
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
					return dicIDs;
				}
			}
			return null;
		}

		void ThrowExceptionIfPropertyHasError(JProperty property)
		{
			if (property.Name == "error")
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

		//5835 - Rechica
		// 282 - Minsk
		public async Task<TotalListOfUsers> SearchPeople(string query, long cityCode, long offset = 0, int count = 100)
		{
			//String template = "https://api.vk.com/method/METHOD_NAME?PARAMETERS&access_token=ACCESS_TOKEN"
			string token = App.User.Token;
			string parameters = "q=" + WebUtility.UrlEncode(query) + "&sort=1&fields=" + User.PicturesJoint + ",uid,first_name,last_name,can_write_private_message" + "&sex=1&age_from=19&age_to=34" + "&country=3&city=" + cityCode + "&offset=" + offset + "&count=" + count;
			string templatePart = UsersSearchAPIKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelOperation(UsersSearchAPIKey);
			var totalList = new TotalListOfUsers();
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

					// JsonConvert.DeserializeObject<TotalListOfUsersWrapper>(jsonString);

					var index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index + 1);

					index = jsonString.IndexOf('{');
					if (index == -1)
					{
						//TODO: analyze response and keep total amonunt of found users.
						throw new ItemNotFoundException("Users were not found!");
						//return new List<User>();
					}

					var index2 = jsonString.IndexOf('[');
					var index3 = jsonString.IndexOf(',');
					var numberStr = jsonString.Substring(index2 + 1, index3 - index2 - 1);

					totalList.Count = long.Parse(numberStr);


					jsonString = jsonString.Substring(index);
					var length = jsonString.Length;

					var finalString = "{response:[" + jsonString;
					var responseUsers = JsonConvert.DeserializeObject<ResponseUsers>(finalString);

					totalList.Items = responseUsers.Items;

					return totalList;
				}
				else
					CancelOperation(UsersSearchAPIKey, false);
			}
			return totalList;
		}

		public async Task<long[]> SendMessageToUsers(string message, long[] userIDs)
		{
			if (userIDs.Length == 0 || message.Length == 0)
			{
				return null;
			}

			string resultIDs = "";

			foreach (var userId in userIDs)
			{
				if (resultIDs.Length != 0)
					resultIDs = string.Concat(resultIDs, ",");
				resultIDs = string.Concat(resultIDs, userId);
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
							else
								ThrowExceptionIfPropertyHasError(property);
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

		public bool CancelIsAGroupMemberDetection(long[] userIDs, long groupId)
		{
			if (userIDs.Length == 0)
				return false;

			var query = GetGroupDetectionTemplate(userIDs, groupId);

			return !((query == null) || CancelOperation(query) == null);
		}

		private string GetGroupDetectionTemplate(long[] userIDs, long groupId)
		{
			if (userIDs.Length == 0)
				return null;

			string resultIDs = "";

			foreach (var userId in userIDs)
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

		private string GetAddOrCreateFriendRequestTemplate(long userId)
		{
			string parameters = "user_id=" + userId;
			string templateKey = AddOrCreateFriendRequestAPIKey + parameters;
			return templateKey;
		}

		public async Task<int> InviteUserToAGroup(long userId, long groupId)
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
							}
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
					return 0;
				}
			}
			return 0;
		}

		public async Task<FriendAddStatus> CreateOrApproveFriendRequest(long userId, string text = null, int? follow = null)
		{

			var templateKey = GetAddOrCreateFriendRequestTemplate(userId);
			if (templateKey == null) return FriendAddStatus.FriendRequestNone;

			string token = App.User.Token;
			string template = templateKey;

			CancelOperation(templateKey);

			if (!string.IsNullOrEmpty(text))
				template += "&text=" + WebUtility.UrlEncode(text);

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
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
				}
				return FriendAddStatus.FriendRequestNone;
			}
		}

		public async Task<UserGroupStatus[]> DetectIfUserIsAGroupMember(long[] userIDs, long groupId)
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
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
					return statuses.ToArray();
				}
			}
			return null;
		}

		#region Messages Methods 

		public async Task<TotalListOfMessages> SearchMessages(string query, long peerId, long offset = 0, long count = 100, DateTime? date = null, int previewLength = int.MinValue)
		{
			Debug.Assert(!string.IsNullOrEmpty(query));
			if (string.IsNullOrEmpty(query))
				throw new ArgumentException("Query is null or empty");

			var dic = new Dictionary<string, object>();
			const string c_TranslateParam = "q";

			if (!string.IsNullOrEmpty(query))
				dic.Add(c_TranslateParam, query);

			dic.Add("peer_Id", peerId);

			//TODO: Use time of VK server ....!!!
			if (date == null)
				date = DateTime.UtcNow;
			//dic.Add("date", date.Value.ToUnixTimestamp());

			dic.Add("offset", offset);
			dic.Add("count", count);
			dic.Add("preview_length", previewLength != int.MinValue ? previewLength : query.Length);

			var set = new HashSet<string>();
			set.Add(c_TranslateParam);

			string key = MessagesSearchKey;
			string token = App.User.Token;
			string searchQuery = key + FormatStringFromDic(dic, set) + "&access_token=" + token;

			key += query ?? string.Empty + string.Format("{0}.{1}.{2}", peerId, offset, count);

			CancelOperation(key);
			using (var client = new HttpClient())
			{

				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[key] = tokenSource;

				var response = await client.GetAsync(searchQuery, tokenSource.Token).ConfigureAwait(false);

				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);


					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();

					foreach (JToken jToken in resultObj)
					{

						if (jToken.Type == JTokenType.Property)
						{
							var property = (JProperty)jToken;
							if (property.Name == "response")
							{

								var value = property.Value;
								if (value.Type == JTokenType.Object)
								{
									var obj = (JObject)value;

									if (obj.HasValues)
									{
										JToken countToken = null;
										if (obj.TryGetValue("count", out countToken) && countToken.Type == JTokenType.Integer)
										{
											var itemsCount = countToken.Value<long>();

											JToken usersToken = null;
											if (obj.TryGetValue("messages", out usersToken) && usersToken.Type == JTokenType.Array)
											{
												var users = usersToken.ToObject<Message[]>();

												var pUsers = users.Where((Message arg) => arg.UserId == peerId).ToArray();

												return new TotalListOfMessages { Count = itemsCount, Items = pUsers };
											}
											else
												Debug.WriteLine("No Users in JSON");
										}
										else
											Debug.WriteLine("No Count in JSON");
									}
								}
								else if (value.Type == JTokenType.Array)
								{
									var array = (JArray)value;

									long userCount = 0;
									var fMessages = new List<Message>();

									foreach (var item in array)
									{
										if (userCount == 0 && item.Type == JTokenType.Integer )
											userCount = item.Value<int>();
										else if (item.Type == JTokenType.Object)
										{
											var message = item.ToObject<Message>();

											if (message.UserId == peerId)
												fMessages.Add(message);
										}
									}


									return new TotalListOfMessages { Count = userCount, Items = fMessages.ToArray() };
								}
								else 
									Debug.WriteLine("Format of json has changed!");
							}
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
				}

			}
			return new TotalListOfMessages();
		}

		#endregion

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
		public async Task<TotalListOfUsers> GroupsGetMembers(long groupId, long offset = 0, long count = 1000, bool sort = true, string[] fields = null)
		{
			string token = App.User.Token;

			List<string> eFields = null;

			if (fields == null || fields.Length == 0)
			{
				eFields = new List<string>() { "first_name", "last_name", "uid", "can_write_private_message" };
				eFields.AddRange(User.Pictures);
			}
			else
				eFields = new List<string>(fields);

			var resultFields = "";

			foreach (string tempId in eFields)
			{
				if (resultFields.Length != 0)
					resultFields = string.Concat(resultFields, ",");
				resultFields = string.Concat(resultFields, tempId);
			}

			string parameters = "group_id=" + groupId + "&sort=" + (sort == true ? "id_asc" : "id_desc") + "&fields=" + resultFields + "&offset=" + offset + "&count=" + count;
			string templatePart = GroupsMembersKey + parameters;
			string template = templatePart + "&access_token=" + token;

			CancelOperation(GroupsMembersKey);

			using (var client = new HttpClient())
			{
				CancellationTokenSource tokenSource = new CancellationTokenSource();
				cacheMap[GroupsMembersKey] = tokenSource;

				var response = await client.GetAsync(template, tokenSource.Token).ConfigureAwait(false);
				CancelOperation(GroupsMembersKey);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);

					var result = JObject.Parse(jsonString);

					var resultObj = result.AsJEnumerable().AsEnumerable();

					foreach (JToken jToken in resultObj)
					{

						if (jToken.Type == JTokenType.Property)
						{
							var property = (JProperty)jToken;
							if (property.Name == "response")
							{

								var value = property.Value;
								if (value.Type == JTokenType.Object)
								{
									var obj = (JObject)value;

									if (obj.HasValues)
									{
										JToken countToken = null;
										if (obj.TryGetValue("count", out countToken) && countToken.Type == JTokenType.Integer)
										{
											var itemsCount = countToken.Value<long>();

											JToken usersToken = null;
											if (obj.TryGetValue("users", out usersToken) && usersToken.Type == JTokenType.Array)
											{
												var users = usersToken.ToObject<User[]>();

												return new TotalListOfUsers { Count = itemsCount, Items = users };
											}
											else
												Debug.WriteLine("No Users in JSON");
										}
										else 
											Debug.WriteLine("No Count in JSON");
									}
								}
								else
									Debug.WriteLine("Format of json has changed!");
							}
							else
								ThrowExceptionIfPropertyHasError(property);
						}
					}
				}
				else
					Debug.WriteLine("Response is failed " + response);
			}
			return new TotalListOfUsers();;
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

	public sealed class ItemNotFoundException : Exception
	{
		public ItemNotFoundException(string message) : base(message) { }
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


