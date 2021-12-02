using System;
using System.Net.Http;

namespace ServiceStack
{
    public static class BlazorClient
    {
        public static HttpMessageHandler MessageHandler { get; set; } = new HttpClientHandler(); 
        
        public static JsonHttpClient Create(string baseUrl, Action<JsonHttpClient> configure = null) 
        {
            var client = new JsonHttpClient(baseUrl) {
                HttpMessageHandler = MessageHandler
            };
            configure?.Invoke(client);
            return client;
        }
    }
}
