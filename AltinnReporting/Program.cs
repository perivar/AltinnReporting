using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AltinnReporting
{
    class Program
    {
        static void Main(string[] args)
        {
            string apiKey = ConfigurationManager.AppSettings["ApiKeyProd"];
            string uri = ConfigurationManager.AppSettings["UriProd"];
            string username = ConfigurationManager.AppSettings["UserName"];
            string password = ConfigurationManager.AppSettings["Password"];
            string origin = ConfigurationManager.AppSettings["Origin"];

            var authCookie = AuthenticateAsync(uri, apiKey, username, password).GetAwaiter().GetResult();
            //var result = GetContentAsync(uri, apiKey, authCookie, origin, "/api/918557261/messages/b246420323").GetAwaiter().GetResult();
            var result = GetContentAsync(uri, apiKey, authCookie, origin, "/api/918557261/messages").GetAwaiter().GetResult();
            //var result2 = PostContentAsync(uri, apiKey, authCookie, origin, "/api/918557261/messages?complete=false").GetAwaiter().GetResult();
        }

        static public async Task<Cookie> AuthenticateAsync(string baseUrl, string apiKey, string username, string password)
        {
            var handler = GetHttpBaseClientHandler();

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/hal+json");
                client.DefaultRequestHeaders.Add("ApiKey", apiKey);

                var authDetails = GetJsonAuthDetails(username, password);
                //var authDetails = GetAuthDetails(username, password);

                var authResult = await client.PostAsync("/api/authentication/authenticatewithpassword", authDetails);

                authResult.EnsureSuccessStatusCode();

                var responseCookies = handler.CookieContainer.GetCookies(client.BaseAddress);
                return GetAuthCookie(responseCookies);
            }
        }

        static public async Task<string> GetContentAsync(string baseUrl, string apiKey, Cookie authCookie, string origin, string uri)
        {
            var handler = GetHttpBaseClientHandler();

            handler.CookieContainer.Add(authCookie);

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/hal+json");
                client.DefaultRequestHeaders.Add("ApiKey", apiKey);
                client.DefaultRequestHeaders.Add("Origin", origin);

                var itemResult = await client.GetAsync(uri);

                itemResult.EnsureSuccessStatusCode();

                return await itemResult.Content.ReadAsStringAsync();
            }
        }

        static public async Task<string> PostContentAsync(string baseUrl, string apiKey, Cookie authCookie, string origin, string uri)
        {
            var handler = GetHttpBaseClientHandler();

            handler.CookieContainer.Add(authCookie);

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/hal+json");
                client.DefaultRequestHeaders.Add("ApiKey", apiKey);
                client.DefaultRequestHeaders.Add("Origin", origin);

                var jsonData = GetJsonFormData();

                var postResult = await client.PostAsync(uri, jsonData);

                postResult.EnsureSuccessStatusCode();

                return await postResult.Content.ReadAsStringAsync();
            }
        }

        static HttpClientHandler GetHttpBaseClientHandler()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                UseDefaultCredentials = false
            };
            return handler;
        }

        static Cookie GetAuthCookie(CookieCollection responseCookies)
        {
            if (responseCookies == null) return null;

            foreach (Cookie cookie in responseCookies)
            {
                string cookieName = cookie.Name;
                string cookieValue = cookie.Value;
                string domain = cookie.Domain;
                string path = cookie.Path;
                DateTime timeStamp = cookie.TimeStamp;

                // only care about the .ASPXAUTH token
                if (cookieName.Equals(".ASPXAUTH"))
                {
                    return cookie;
                }
            }
            return null;
        }

        static FormUrlEncodedContent GetAuthDetails(string username, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("UserName", username),
                    new KeyValuePair<string, string>("UserPassword", password),
            });
            return content;
        }

        static StringContent GetJsonAuthDetails(string username, string password)
        {
            JObject jsonAuthData = new JObject(
                new JProperty("UserName", username),
                new JProperty("UserPassword", password)
            );

            var content = new StringContent(jsonAuthData.ToString(),
                                    Encoding.UTF8,
                                    "application/hal+json"); // CONTENT-TYPE header
            return content;
        }

        static StringContent GetJsonFormData()
        {
            JObject jsonData = new JObject(
                new JProperty("Type", "FormTask"),
                new JProperty("ServiceCode", "123"),
                new JProperty("ServiceEdition", "64"),
                new JProperty("_embedded",
                    new JObject(
                        new JProperty("forms",
                            new JObject(
                                new JProperty("Type", "MainForm"),
                                new JProperty("DataFormatId", "1023"),
                                new JProperty("DataFormatVersion", "1"),
                                new JProperty("FormData", "<Skjema></Skjema>")
                            )
                        ),
                        new JProperty("attachments",
                            new JObject(
                                new JProperty("FileName", "string example 1"),
                                new JProperty("AttachmentType", "string example 2"),
                                new JProperty("Data", "")
                                )
                            )
                        )
                    )
                );

            var content = new StringContent(jsonData.ToString(),
                                    Encoding.UTF8,
                                    "application/hal+json"); // CONTENT-TYPE header
            return content;
        }

        static string GetCookieString(CookieContainer cookieContainer, string uri)
        {
            // Generate a cookie string like: "cookie1=value1; cookie2=value2"
            var str = new StringBuilder();
            foreach (Cookie c in cookieContainer.GetCookies(new Uri(uri)))
            {
                string thisCookie = c.Name.ToUpper() + "=" + c.Value + ";";
                str.Append(thisCookie);
            }
            return str.ToString();
        }
    }
}
