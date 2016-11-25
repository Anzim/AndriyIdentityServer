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
        private static string _clientAccessToken;
        private static string _tokenEndpoint;
        public static void Main(string[] args)
        {
            var task = GetTokenEndpoint().ContinueWith(te =>
            {
                if (!te.IsCompleted)
                {
                    Console.WriteLine($"Can not get token endpoint: {te.Exception?.Message}"); 
                    return;
                }
                GetClientToken().ContinueWith(t =>
                {
                    if (!t.IsCompleted)
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
            var r = GetAliceToken().ContinueWith(t => 
            {
                if (!t.IsCompleted)
                {
                    Console.WriteLine($"Can not get access token: {t.Exception?.Message}");
                    return null;
                }
                return t.Result;
                //GetIdentity(_clientAccessToken).ContinueWith(ContinuationAction);
            });
        }

        static async Task GetTokenEndpoint()
        {
            // discover endpoints from metadata
            var disco = await DiscoveryClient.GetAsync("http://localhost:5000");
            _tokenEndpoint = disco.TokenEndpoint;
        }
        static async Task GetClientToken()
        {
            // request token
            var tokenClient = new TokenClient(_tokenEndpoint, "client", "secret");
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

        static async Task<string> GetAliceToken()
        {
            // request token
            var token = new TokenClient(_tokenEndpoint, "ro.client", "ro.secret");
            var tokenResponse = await token.RequestResourceOwnerPasswordAsync("alice", "password", "api1");

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
