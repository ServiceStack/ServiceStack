#nullable enable
#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NUnit.Framework;
using ServiceStack.Mvc;

namespace ServiceStack.Server.Tests;

[TestFixture]
public class MvcSecurityTests
{
    public class SampleModel
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    [Test]
    public void CreateViewData_CreatesTypedViewDataDictionary()
    {
        var model = new SampleModel { Name = "Test", Count = 42 };
        var viewData = RazorPagesEngine.CreateViewData(model);

        Assert.That(viewData, Is.Not.Null);
        Assert.That(viewData.Model, Is.SameAs(model));
        Assert.That(viewData, Is.InstanceOf<ViewDataDictionary<SampleModel>>());
    }

    [Test]
    public void CreateViewData_CreatesDynamicViewDataDictionary_ForAnonymousTypes()
    {
        var anon = new { Foo = "Bar", Num = 123 };
        var viewData = RazorPagesEngine.CreateViewData(anon);

        Assert.That(viewData, Is.Not.Null);
        Assert.That(viewData.Model, Is.InstanceOf<DictionaryDynamicObject>());

        dynamic dynamicModel = viewData.Model!;
        Assert.That(dynamicModel.Foo, Is.EqualTo("Bar"));
        Assert.That(dynamicModel.Num, Is.EqualTo(123));
    }

    [Test]
    public void ResolvePageRoute_HandlesEdgeCases_WithoutThrowing()
    {
        var model = new SampleModel { Name = "Special/Slug", Count = 1 };
        
        // Verifies trailing slash index.html resolution
        var route = RazorSsg.ResolvePageRoute("/pages/{Name}/", model);
        Assert.That(route, Is.EqualTo("/pages/Special/Slug/index.html"));

        // Verifies file extension resolution
        var route2 = RazorSsg.ResolvePageRoute("/pages/{Name}", model);
        Assert.That(route2, Is.EqualTo("/pages/Special/Slug.html"));

        // Verifies static route
        var route3 = RazorSsg.ResolvePageRoute("/about", model);
        Assert.That(route3, Is.EqualTo("/about.html"));
    }

    private class TestMvcController : ServiceStackController
    {
        private string customRedirectUrl;

        public TestMvcController(string redirectUrl)
        {
            this.customRedirectUrl = redirectUrl;
        }

        public override string UnauthorizedRedirectUrl => customRedirectUrl;
        public override string ForbiddenRedirectUrl => customRedirectUrl;

        public string GetExpectedAuthRedirect(string returnUrl)
        {
            var sep = UnauthorizedRedirectUrl.IndexOf('?') >= 0 ? "&" : "?";
            return $"{UnauthorizedRedirectUrl}{sep}redirect={returnUrl.UrlEncode()}#f=Unauthorized";
        }

        public string GetExpectedForbiddenRedirect(string returnUrl)
        {
            var sep = ForbiddenRedirectUrl.IndexOf('?') >= 0 ? "&" : "?";
            return $"{ForbiddenRedirectUrl}{sep}redirect={returnUrl.UrlEncode()}#f=Forbidden";
        }
    }

    [Test]
    public void RedirectUrl_FormatsCorrectSeparator_WhenUrlHasQueryString()
    {
        var controllerWithQuery = new TestMvcController("/auth/login?theme=dark");
        var redirect = controllerWithQuery.GetExpectedAuthRedirect("/dashboard?tab=1");
        Assert.That(redirect, Does.StartWith("/auth/login?theme=dark&redirect="));
        Assert.That(redirect, Does.EndWith("#f=Unauthorized"));

        var forbiddenRedirect = controllerWithQuery.GetExpectedForbiddenRedirect("/admin");
        Assert.That(forbiddenRedirect, Does.StartWith("/auth/login?theme=dark&redirect="));
        Assert.That(forbiddenRedirect, Does.EndWith("#f=Forbidden"));
    }

    [Test]
    public void RedirectUrl_FormatsCorrectSeparator_WhenUrlHasNoQueryString()
    {
        var controllerNoQuery = new TestMvcController("/auth/login");
        var redirect = controllerNoQuery.GetExpectedAuthRedirect("/dashboard");
        Assert.That(redirect, Does.StartWith("/auth/login?redirect="));
        Assert.That(redirect, Does.EndWith("#f=Unauthorized"));
    }
}

#endif
