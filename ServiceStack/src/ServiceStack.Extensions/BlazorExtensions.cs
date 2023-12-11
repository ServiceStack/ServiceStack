#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Web;
using System;
using System.Net;
using System.Net.Http;

namespace ServiceStack;

public static class BlazorExtensions
{
    public static IHttpClientBuilder AddBlazorServerIdentityApiClient(this IServiceCollection services, string baseUrl, Action<IdentityApiClientConfig>? configure = null)
    {
        var config = new IdentityApiClientConfig();
        configure?.Invoke(config);

        return services
            .AddScoped<IClientFactory>(c =>
            {
                return new IdentityClientFactory(
                    c.GetRequiredService<JsonApiClient>(), 
                    c.GetRequiredService<IServiceGatewayFactory>(), 
                    baseUrl)
                {
                    HttpContext = c.GetService<IHttpContextAccessor>()?.HttpContext,
                    RequestFactory = c.GetService<IGatewayRequestFactory>(),
                };
            })
            .AddSingleton<IServiceGatewayFactory, IdentityAuthServiceGatewayFactory>()
            .AddHttpClient<JsonApiClient>(client => {
                client.BaseAddress = new Uri(baseUrl);
                config.HttpClientFilter?.Invoke(client);
            })
            .ConfigureHttpMessageHandlerBuilder(h =>
            {
                var to = HttpUtils.HttpClientHandlerFactory();
                to.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                config.HttpClientHandlerFilter?.Invoke(to);
            });
    }
}

public class IdentityApiClientConfig
{
   public Action<HttpClient>? HttpClientFilter { get; set; }
   public Action<HttpClientHandler>? HttpClientHandlerFilter { get; set; }
}

public class IdentityClientFactory : IClientFactory
{
    public JsonApiClient Client { get; }
    public IServiceGatewayFactory GatewayFactory { get; }
    public IGatewayRequestFactory? RequestFactory { get; set; }
    public string BaseUrl { get; }
    public HttpContext? HttpContext { get; set; }
    public IdentityClientFactory(JsonApiClient client, IServiceGatewayFactory gatewayFactory, string baseUrl)
    {
        Client = client;
        GatewayFactory = gatewayFactory;
        BaseUrl = baseUrl;
    }
    public JsonApiClient GetClient()
    {
        return Client;
    }
    public IServiceGateway GetGateway()
    {
        var request = HttpContext?.ToRequest() ?? new BasicHttpRequest
        {
            AbsoluteUri = BaseUrl,
            RawUrl = BaseUrl,
            PathInfo = "/",
        };
        var gatewayRequest = RequestFactory?.Create(request) ?? GatewayRequest.Create(request);
        var gateway = GatewayFactory.GetServiceGateway(gatewayRequest);
        return gateway;
    }
}