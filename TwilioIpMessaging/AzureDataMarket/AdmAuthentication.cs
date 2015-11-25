using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AzureDataMarket
{
    public class AdmAuthentication
    {
        public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";

        private string clientId;
        private string clientSecret;
        private List<KeyValuePair<string, string>> request;

        private AdmAccessToken token;
        private Timer accessTokenRenewer;
        
        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        private AdmAuthentication(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        
            request = new List<KeyValuePair<string, string>> { 
                new KeyValuePair<string,string>("grant_type", "client_credentials"),
                new KeyValuePair<string,string>("client_id", clientId),
                new KeyValuePair<string,string>("client_secret", clientSecret),
                new KeyValuePair<string,string>("scope", "http://api.microsofttranslator.com")
            };
            
            //renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
        }

        private static AdmAuthentication _instance;
        public static AdmAuthentication GetInstance(string clientId, string clientSecret)
        {
            if (_instance == null)
            {
                _instance = new AdmAuthentication(clientId, clientSecret);
            }

            return _instance;
        }

        public async Task<AdmAccessToken> GetAccessToken()
        {
            if (this.token == null)
            {
                await RenewAccessToken();
            }

            return this.token;
        }

        private async Task RenewAccessToken()
        {
            AdmAccessToken newAccessToken = await HttpPost(DatamarketAccessUri, this.request);
            
            //swap the new token with old one
            //Note: the swap is thread unsafe
            this.token = newAccessToken;

            Console.WriteLine(string.Format("Renewed token for user: {0} is: {1}", this.clientId, this.token.access_token));
        }

        private async void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                await RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private async Task<AdmAccessToken> HttpPost(string DatamarketAccessUri, IList<KeyValuePair<string,string>> requestDetails)
        {
            //Prepare OAuth request 
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(requestDetails);

            try
            {
                var result = await client.PostAsync(DatamarketAccessUri, content);
                var accessToken = await result.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<AdmAccessToken>(accessToken);
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
    }
}
