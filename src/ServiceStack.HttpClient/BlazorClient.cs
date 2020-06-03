using System.Net.Http;

namespace ServiceStack
{
    public static class BlazorClient
    {
        public static HttpMessageHandler MessageHandler { get; set; } = new HttpClientHandler(); 
        
        public static JsonHttpClient Create(string baseUrl) => new JsonHttpClient(baseUrl) {
            HttpMessageHandler = MessageHandler
        };
    }
}