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
    /// <summary>
    /// Register an IClientFactory for Blazor Server Apps using ASP.NET Core Identity Auth.
    /// In Process Gateway requests use the Session converted from the circuit's AuthenticationStateProvider User.
    /// </summary>
    public static IHttpClientBuilder AddBlazorServerIdentityApiClient(this IServiceCollection services, string baseUrl, Action<IdentityApiClientConfig>? configure = null)
    {
        var config = new IdentityApiClientConfig();
        configure?.Invoke(config);

        services.AddSingleton<IServiceGatewayFactory, IdentityAuthServiceGatewayFactory>();
        return services.AddBlazorServerClaimsApiClient(baseUrl, config);
    }
}

public class IdentityApiClientConfig : BlazorServerApiClientConfig
{
}

[Obsolete("Reads the User from the HttpContext captured when the Blazor Server circuit started, use BlazorServerClaimsClientFactory instead")]
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