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

namespace VKPeopleInviter
{
    public sealed class VKManager
    {
        private int clientId = 5537512;
        private string clientSecretToken = "E9x6ywxHcYnnqf3ZXtjd";
     

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
			if (duration != 0) {
				client.SetSessionData(token, userId, duration);
			}
			else {
				client.SetSessionData(token, userId);
			}

		}

		public async Task<Response<ItemsList<ModernDev.InTouch.User>>> searchPeople(string query) 
		{
			UsersSearchParams searchParams = new UsersSearchParams();
			searchParams.Country = 3; //Belarus
			searchParams.AgeFrom = 18;
			searchParams.AgeTo = 28;
			searchParams.Query = query;
			//Rechica - 5835
			//Minsk - 282
			searchParams.City = 282;
			searchParams.SortByDate = true;
			return await client.Users.Search(searchParams);
		}

		public async Task<List<User>> SearchPeople(string query,int offset = 0, int count = 100)
		{
			//String template = "https://api.vk.com/method/METHOD_NAME?PARAMETERS&access_token=ACCESS_TOKEN"
			String token = this.client.Session.AccessToken;
			string parameters = "q=" + query + "&sort=1&fields=photo_100,uid,first_name,last_name" + "&sex=1&age_from=19&age_to=34" + "&country=3&city=282" + "&offset=" + offset + "&count=" + count;
			String template = "https://api.vk.com/method/users.search?" + parameters + "&access_token=" + token;
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(template).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
					var index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index+1);

					index = jsonString.IndexOf('{');
					jsonString = jsonString.Substring(index);
					var length = jsonString.Length;

					var finalString = "{response:[" + jsonString;
						var responseUsers = JsonConvert.DeserializeObject<ResponseUsers>(finalString);
						return new List<User>(responseUsers.users);
				}
			}
			return null;
		}

		public async Task<Int64[]> SendMessageToUsers(string message, string[] userIDs)
		{
			if (userIDs.Length == 0 || message.Length == 0 ) {
				return null;
			}

			string resultIDs = "";

			foreach (string userId in userIDs) {
				if (resultIDs.Length != 0)
					resultIDs = String.Concat(resultIDs, ",");
				resultIDs = String.Concat(resultIDs,userId);
			}

			String token = this.client.Session.AccessToken;
			string parameters = "user_ids=" + resultIDs + "&message=" + WebUtility.UrlEncode(message) + "&oauth=2";

			String template = "https://api.vk.com/method/messages.send?" + parameters + "&access_token=" + token;
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(template).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					var content = response.Content;
					Debug.WriteLine("Content " + content.ToString());

					string jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);

					var result = JObject.Parse(jsonString);


					//var jsonValue = JsonValue.Parse(jsonString);

					var jsonResponse = result["response"].Value<JArray>();
					Int64[] ids = jsonResponse.Select(arg1 => (Int64)arg1).ToArray() ;
					return ids;
				}
			}
			return null;
		}
	}
}
