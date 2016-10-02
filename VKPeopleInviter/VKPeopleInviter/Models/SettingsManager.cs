using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace VKPeopleInviter
{
	public class SettingsManager
	{
		Application app;
		public City[] SelectedCities { get; private set;}
		public string InvitationText { get; private set;}

		public SettingsManager(Application app)
		{
			this.app = app;
			object arrayObj;
			var result = app.Properties.TryGetValue(Constants.CitiesKey, out arrayObj);

			var array = arrayObj as City[];

			if (result == false || array.Length == 0)
			{
				app.Properties[Constants.CitiesKey] = this.SupportedCitiesPrivate;
				app.SavePropertiesAsync();
				array = (City[])app.Properties[Constants.CitiesKey];
			}
			Debug.Assert(array.Length != 0);
			SelectedCities = array;

			object invitationObj;

			result = app.Properties.TryGetValue(Constants.InvitationTemplateKey, out invitationObj);

			string invitation = invitationObj as string;
			if (result == false || invitation.Length == 0)
			{
				app.Properties[Constants.InvitationTemplateKey] = this.InvitationTextPrivate;
				app.SavePropertiesAsync();
				invitation = (string)app.Properties[Constants.InvitationTemplateKey];
			}
			InvitationText = invitation;
		}

		//TODO: Move it into appropriate place, for instance JSON file...
		City[] SupportedCitiesPrivate
		{
			get
			{
				return new City[] { new City("Minsk", "282"), new City("Rechica", "5835") };
			}
		}

		string InvitationTextPrivate
		{
			get
			{
				return "Здравствуйте, от лица мастера по наращиванию ресниц \n и по шугарингу(депиляция сахаром),буду рад видеть Вас в её \n группе https://m.vk.com/beyesby \n Заранее спасибо Не сочтите за спам )";
			}
		}

	}
}
