using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ServiceStack.Blazor;

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