using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;

namespace AndriyClient
{
    
    public class Program
    {
        static string _identityServer = "https://identity.mob-dev.stream/";  //"http://localhost:5000"
        private static string _clientAccessToken;
        private static string _tokenEndpoint;
        public static void Main(string[] args)
        {
            GetTokenEndpoint().ContinueWith(te =>
            {
                if (te.Status != TaskStatus.RanToCompletion)
                {
                    Console.WriteLine($"Can not get token endpoint: {te.Exception?.Message}"); 
                    return;
                }
                GetClientToken().ContinueWith(t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion)
                    {
                        Console.WriteLine($"Can not get access token: {te.Exception?.Message}");
                        return;
                    }
                    GetIdentity(_clientAccessToken).ContinueWith(ContinuationAction);
                });
            });
                
            Console.ReadLine();
        }

        private static void ContinuationAction(Task task)
        {
            if (task.Status != TaskStatus.RanToCompletion)
            {
                Console.WriteLine($"Can not get identity: {task.Exception?.Message}");
            }
            var r = GetAnzimToken().ContinueWith(t => 
            {
                if (t.Status != TaskStatus.RanToCompletion)
                {
                    Console.WriteLine($"Can not request resource owner with password for user anzim@list.ru: {t.Exception?.Message}");
                    return null;
                }
                return t.Result;
            });
        }

        static async Task GetTokenEndpoint()
        {
            // discover endpoints from metadata
            var disco = await DiscoveryClient.GetAsync(_identityServer);
            _tokenEndpoint = disco.TokenEndpoint;
            if (disco.IsError)
            {
                throw new Exception(disco.ErrorType + ": " + disco.Error);
            }
        }
        static async Task GetClientToken()
        {
            // request token
            var tokenClient = new TokenClient(_tokenEndpoint, "client", "secret");
            Console.WriteLine("Getting identity");
            var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api1");

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }
            _clientAccessToken = tokenResponse.AccessToken;

            Console.WriteLine(tokenResponse.Json);
        }

        static async Task GetIdentity(string token)
        {
            // call api
            var client = new HttpClient();
            client.SetBearerToken(token);

            var response = await client.GetAsync("http://localhost:5001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(JArray.Parse(content));
        }

        static async Task<string> GetAnzimToken()
        {
            // request resource
            Console.WriteLine($"Requesting resource owner with password for user anzim@list.ru");
            var tokenClient = new TokenClient(_tokenEndpoint, "ro.client", "ro.secret");
            var tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync("anzim@list.ru", "Azo+250472-", "api1");

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return null;
            }

            Console.WriteLine(tokenResponse.Json);
            return tokenResponse.AccessToken;
        }
    }
}
