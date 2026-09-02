using System.Collections.Generic;
using System.Security.Claims;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Blazor;
using ServiceStack.Blazor.Components;

namespace MyApp.Tests;

public class TestAuthComponent : AuthBlazorComponentBase
{
    public void SetUser(ClaimsPrincipal? principal)
    {
        User = principal;
    }

    public bool TestCanAccess(MetadataOperationType op) => CanAccess(op);
}

[TestFixture]
public class BlazorSecurityTests
{
    [Test]
    public void NavigationUtils_IsLocalUrl_validates_correctly()
    {
        Assert.That("/".IsLocalUrl(), Is.True);
        Assert.That("/dashboard".IsLocalUrl(), Is.True);
        Assert.That("/app/path?query=1".IsLocalUrl(), Is.True);

        Assert.That("".IsLocalUrl(), Is.False);
        Assert.That(((string?)null).IsLocalUrl(), Is.False);
        Assert.That("https://evil.com".IsLocalUrl(), Is.False);
        Assert.That("http://evil.com".IsLocalUrl(), Is.False);
        Assert.That("//evil.com".IsLocalUrl(), Is.False);
        Assert.That("/\\evil.com".IsLocalUrl(), Is.False);
        Assert.That("javascript:alert(1)".IsLocalUrl(), Is.False);
    }

    [Test]
    public void AuthBlazorComponentBase_enforces_required_roles_and_permissions()
    {
        var component = new TestAuthComponent();

        var opRequiresAdmin = new MetadataOperationType
        {
            RequiresAuth = true,
            RequiredRoles = new List<string> { "Admin" },
        };

        var regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "User"),
        }, "Server Authentication"));

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "Server Authentication"));

        // User without "Admin" role must be denied access
        component.SetUser(regularUser);
        Assert.That(component.TestCanAccess(opRequiresAdmin), Is.False);

        // User with "Admin" role must be granted access
        component.SetUser(adminUser);
        Assert.That(component.TestCanAccess(opRequiresAdmin), Is.True);
    }

    [Test]
    public void BlazorUtils_FormatValueAsHtml_encodes_keys_and_scalars()
    {
        var dict = new Dictionary<string, object?>
        {
            ["<script>alert(1)</script>"] = "<b>safe</b>"
        };

        var html = BlazorUtils.FormatValueAsHtml(dict);
        Assert.That(html, Does.Not.Contain("<script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    [Test]
    public void HtmlUtils_HtmlDump_caps_recursion_depth()
    {
        var root = new Dictionary<string, object>();
        var current = root;
        for (int i = 0; i < 50; i++)
        {
            var next = new Dictionary<string, object>();
            current["child"] = next;
            current = next;
        }

        var dump = HtmlUtils.HtmlDump(root);
        Assert.That(dump, Does.Contain("..."));
    }
}
