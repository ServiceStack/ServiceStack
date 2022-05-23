using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.Blazor
{
    public static class BlazorClient
    {
        public static Func<HttpMessageHandler>? MessageHandlerFactory { get; set; } =
            () => new EnableCorsMessageHandler();
        
        public static JsonApiClient Create(string baseUrl, Action<JsonApiClient>? configure = null) 
        {
            var client = new JsonApiClient(baseUrl) {
                HttpMessageHandler = MessageHandlerFactory?.Invoke()
            };
            configure?.Invoke(client);
            return client;
        }
        
        public static IHttpClientBuilder AddBlazorApiClient(this IServiceCollection services, string baseUrl)
        {
            services.AddTransient<EnableCorsMessageHandler>();
            return services.AddHttpClient<JsonApiClient>(client => client.BaseAddress = new Uri(baseUrl))
                .AddHttpMessageHandler<EnableCorsMessageHandler>();
        }
    }
}
