using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Blazor;

namespace MyApp.Auth;

public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
{
    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackStateProvider> log)
        : base(client, log) { }
}
