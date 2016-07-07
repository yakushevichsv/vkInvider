using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernDev;
using ModernDev.InTouch;
using System.Diagnostics;

namespace VKPeopleInviter
{
    class VKManager
    {
        private int clientId = 123456;
        private string clientSecretToken = "";
        private string accessToken = "";


        private InTouch client { get; set; }
        VKManager()
        {
            client = new InTouch(false, true);
            client.SetApplicationSettings(clientId, clientSecretToken);
            client.AuthorizationFailed += Client_AuthorizationFailed;
            client.CaptchaNeeded += Client_CaptchaNeeded;
        }

        void Execute()
        {
        }

        private void Client_CaptchaNeeded(object sender, ResponseError e)
        {
            Debug.WriteLine(e.Message);
        }

        private void Client_AuthorizationFailed(object sender, ResponseError e)
        {
            Debug.WriteLine(e.Message);
        }
    }
}
