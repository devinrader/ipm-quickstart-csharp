using AzureDataMarket;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using Twilio.IpMessaging;

namespace TwilioIpMessaging.Controllers
{
    public class HomeController : Controller
    {
        private AdmAuthentication _admAuth;

        public HomeController()
        {
            _admAuth = AdmAuthentication.GetInstance(
                ConfigurationManager.AppSettings["TranslatorClientId"],
                ConfigurationManager.AppSettings["TranslatorClientSecret"]);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public async Task<ActionResult> MessageAdded(string To, string Body, string From)
        {
            System.Diagnostics.Debug.WriteLine(Body);

            Body = await Translate(Body, "en", "es");

            System.Diagnostics.Debug.WriteLine(Body);

            var client = new IpMessagingClient(
                ConfigurationManager.AppSettings["TwilioApiKey"],
                ConfigurationManager.AppSettings["TwilioApiSecret"]);

            var result = client.CreateMessage(
                ConfigurationManager.AppSettings["TwilioIpmServiceSid"],
                To, From, Body);

            if (result.RestException != null)
            {
                System.Diagnostics.Debug.WriteLine(result.RestException.Message);
            }

            return new HttpStatusCodeResult(403);
        }

        public ActionResult ConfigureService()
        {
            var client = new IpMessagingClient(
                ConfigurationManager.AppSettings["TwilioApiKey"],
                ConfigurationManager.AppSettings["TwilioApiSecret"]);

            client.UpdateService(ConfigurationManager.AppSettings["TwilioIpmServiceSid"],
                null, null, null, null, 5,
                new Dictionary<string, string> {
                { "Webhooks.OnMessageSend.Url", "http://[YOUR_NGROK_URL]/Home/MessageAdded" },
                { "Webhooks.OnMessageSend.Method", "POST" },
                { "Webhooks.OnMessageSend.Format", "XML" }
                });

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        private string translatorUriTemplate = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text={0}&from={1}&to={2}";

        private async Task<string> Translate(string body, string fromLanguage, string toLanguage)
        {
            var token = await _admAuth.GetAccessToken();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token.access_token);

            string uri = string.Format(translatorUriTemplate,
                System.Web.HttpUtility.UrlEncode(body),
                fromLanguage,
                toLanguage);

            var result = await client.GetStringAsync(uri);
            return XDocument.Parse(result).Root.Value;
        }

    }
}