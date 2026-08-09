#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class ChatSpaFallbackTests
{
    class AppHost() : AppHostBase(nameof(ChatSpaFallbackTests), typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }

    [Test]
    public async Task Chat_route_takes_precedence_over_Node_SPA_fallback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddPlugin(new ChatFeature { RequireAuth = false });

        await using var app = builder.Build();
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        app.MapFallbackToNode(new NodeProxy("http://127.0.0.1:1"));
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var response = await client.GetAsync("/chat");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
        Assert.That(html, Does.Contain("<title>llms.py</title>"));

        var fallbackResponse = await client.GetAsync("/react-route");
        Assert.That(fallbackResponse.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));

        await app.StopAsync();
    }
}

#endif
