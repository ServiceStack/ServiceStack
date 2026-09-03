#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Api.OpenApi;
using ServiceStack.Api.OpenApi.Specification;
using ServiceStack.Host.Handlers;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

[Route("/test-items/{Id}")]
[Route("/test-wildcard/{Path*}")]
[Route("/test-constrained/{Code:int}")]
public class TestOpenApiItemRequest : IReturn<TestOpenApiItemResponse>
{
    public int Id { get; set; }
    public string? Path { get; set; }
    public int Code { get; set; }
    public string? QueryParam { get; set; }
}

public class TestOpenApiItemResponse
{
    public string? Result { get; set; }
    public GenericDataHolder<string>? Data { get; set; }
}

public class GenericDataHolder<T>
{
    public T? Value { get; set; }
}

public class TestOpenApiItemService : Service
{
    public object Any(TestOpenApiItemRequest request) => new TestOpenApiItemResponse();
}

[TestFixture]
[NonParallelizable]
public class OpenApiSecurityAndBugTests
{
    [Test]
    public void SchemaFilter_Does_Not_Throw_NRE_When_Responses_Null()
    {
        using var appHost = new BasicAppHost(typeof(TestOpenApiItemService).Assembly)
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(new OpenApiFeature
                {
                    SchemaFilter = schema =>
                    {
                        schema.Description = (schema.Description ?? "") + "_filtered";
                    }
                });
            }
        }.Init();

        var service = appHost.Container.Resolve<OpenApiService>();
        service.Request = new MockHttpRequest { PathInfo = "/openapi" };

        Assert.DoesNotThrow(() =>
        {
            var res = service.Get(new OpenApiSpecification()) as HttpResult;
            Assert.That(res, Is.Not.Null);
            var decl = res!.Response as OpenApiDeclaration;
            Assert.That(decl, Is.Not.Null);
        });
    }

    [Test]
    public void OpenApi_Path_Parameters_Match_Wildcards_And_Constraints()
    {
        using var appHost = new BasicAppHost(typeof(TestOpenApiItemService).Assembly)
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(new OpenApiFeature());
            }
        }.Init();

        var service = appHost.Container.Resolve<OpenApiService>();
        service.Request = new MockHttpRequest { PathInfo = "/openapi" };
        var res = (HttpResult)service.Get(new OpenApiSpecification());
        var decl = (OpenApiDeclaration)res.Response;

        var wildcardPath = decl.Paths["/test-wildcard/{Path*}"];
        Assert.That(wildcardPath, Is.Not.Null);
        var wildcardParam = wildcardPath.Post?.Parameters?.FirstOrDefault(p => p.Name == "Path")
            ?? wildcardPath.Get?.Parameters?.FirstOrDefault(p => p.Name == "Path");
        Assert.That(wildcardParam, Is.Not.Null);
        Assert.That(wildcardParam!.In, Is.EqualTo("path"));

        var constrainedPath = decl.Paths["/test-constrained/{Code:int}"];
        Assert.That(constrainedPath, Is.Not.Null);
        var constrainedParam = constrainedPath.Post?.Parameters?.FirstOrDefault(p => p.Name == "Code")
            ?? constrainedPath.Get?.Parameters?.FirstOrDefault(p => p.Name == "Code");
        Assert.That(constrainedParam, Is.Not.Null);
        Assert.That(constrainedParam!.In, Is.EqualTo("path"));
    }

    [Test]
    public void ResourceFilterPattern_Has_Timeout_And_Filters_Resources()
    {
        var feature = new OpenApiFeature
        {
            ResourceFilterPattern = "^/test-items"
        };

        using var appHost = new BasicAppHost(typeof(TestOpenApiItemService).Assembly)
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(feature);
            }
        }.Init();

        Assert.That(feature.ResourceFilterRegex, Is.Not.Null);
        Assert.That(feature.ResourceFilterRegex.MatchTimeout, Is.EqualTo(TimeSpan.FromSeconds(1)));

        var service = appHost.Container.Resolve<OpenApiService>();
        service.Request = new MockHttpRequest { PathInfo = "/openapi" };
        var res = (HttpResult)service.Get(new OpenApiSpecification());
        var decl = (OpenApiDeclaration)res.Response;

        Assert.That(decl.Paths.ContainsKey("/test-items/{Id}"), Is.True);
        Assert.That(decl.Paths.ContainsKey("/test-wildcard/{Path*}"), Is.False);
    }

    [Test]
    public void SwaggerUI_Sanitizes_ServiceName_And_Urls()
    {
        var feature = new OpenApiFeature
        {
            LogoHref = "javascript:alert(1)",
            LogoUrl = "javascript:alert(2)\" onerror=\"alert(3)"
        };

        using var appHost = new BasicAppHost(typeof(TestOpenApiItemService).Assembly)
        {
            ConfigureAppHost = host =>
            {
                host.ServiceName = "<script>alert('xss')</script>";
                host.Plugins.Add(feature);
            }
        }.Init();

        var handler = appHost.CatchAllHandlers
            .Select(h => h(new MockHttpRequest { PathInfo = "/swagger-ui/" }))
            .FirstOrDefault(h => h is CustomResponseHandler) as CustomResponseHandler;

        Assert.That(handler, Is.Not.Null);

        var mockReq = new MockHttpRequest { PathInfo = "/swagger-ui/" };
        var mockRes = new MockHttpResponse();
        var html = (string)handler!.Action(mockReq, mockRes);

        Assert.That(html.Contains("<script>alert('xss')</script>"), Is.False, "ServiceName must be HTML-encoded");
        Assert.That(html.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;") 
            || html.Contains("&lt;script&gt;alert('xss')&lt;/script&gt;"), Is.True);
        Assert.That(html.Contains("javascript:"), Is.False, "Dangerous URL schemes must be filtered");
    }

    [Test]
    public void OpenApi_Definitions_Handles_Generic_Types_Consistently()
    {
        using var appHost = new BasicAppHost(typeof(TestOpenApiItemService).Assembly)
        {
            ConfigureAppHost = host =>
            {
                host.Plugins.Add(new OpenApiFeature());
            }
        }.Init();

        var service = appHost.Container.Resolve<OpenApiService>();
        service.Request = new MockHttpRequest { PathInfo = "/openapi" };
        var res = (HttpResult)service.Get(new OpenApiSpecification());
        var decl = (OpenApiDeclaration)res.Response;

        // GenericDataHolder<string> definition key should use sanitized ref (no < or >)
        Assert.That(decl.Definitions.Keys.Any(k => k.Contains("<") || k.Contains(">")), Is.False);
        var genericKey = decl.Definitions.Keys.FirstOrDefault(k => k.StartsWith("GenericDataHolder_"));
        Assert.That(genericKey, Is.Not.Null);
    }
}
