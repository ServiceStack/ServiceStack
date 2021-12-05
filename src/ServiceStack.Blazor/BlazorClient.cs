using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.Blazor
{
    public static class BlazorClient
    {
        public static Func<HttpMessageHandler>? MessageHandlerFactory { get; set; } =
            () => new EnableCorsMessageHandler();
        
        public static JsonHttpClient Create(string baseUrl, Action<JsonHttpClient>? configure = null) 
        {
            var client = new JsonHttpClient(baseUrl) {
                HttpMessageHandler = MessageHandlerFactory?.Invoke()
            };
            configure?.Invoke(client);
            return client;
        }
    }
    
    /// <summary>
    /// Required to enable CORS requests
    /// </summary>
    public class EnableCorsMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return base.SendAsync(request, cancellationToken);
        }
    }

    public class JsonApiClient : JsonHttpClient
    {
        public static string? BasePath = "/api";

        public JsonApiClient(HttpClient httpClient)
        {
            this.HttpClient = httpClient;
            this.SetBaseUri(httpClient.BaseAddress?.ToString() ?? "/");
            if (BasePath != null)
                this.WithBasePath(BasePath);
        }
    }

    public static class JsonApiClientUtils
    {
        public static IHttpClientBuilder AddApiClient(this IServiceCollection services, string baseUrl)
        {
            return services.AddHttpClient<JsonApiClient>(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<EnableCorsMessageHandler>();
        }
    }
}
