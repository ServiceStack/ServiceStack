#nullable enable

using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;

namespace ServiceStack.Extensions.Tests;

public class IdentityChatAuthTests
{
    static BasicRequest CreateRequest(AuthUserSession session)
    {
        var request = new BasicRequest();
        request.Items[Keywords.Session] = session;
        return request;
    }

    [Test]
    public async System.Threading.Tasks.Task Falls_back_to_authenticated_ServiceStack_session()
    {
        var auth = new IdentityChatAuth(new ChatFeature { RequireAuth = true });
        var request = CreateRequest(new AuthUserSession
        {
            IsAuthenticated = true,
            UserAuthId = "1",
            UserAuthName = "user@email.com",
            DisplayName = "Test User",
            ProfileUrl = "/profiles/1",
            AuthProvider = "credentials",
            Roles = ["Manager"],
        });

        Assert.That(auth.GetUserName(request), Is.EqualTo("user@email.com"));
        Assert.That(auth.CheckAuth(request).IsAuthenticated, Is.True);

        JsonObject? info = await auth.GetAuthInfoAsync(request);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!["userId"]!.GetValue<string>(), Is.EqualTo("1"));
        Assert.That(info["userName"]!.GetValue<string>(), Is.EqualTo("user@email.com"));
        Assert.That(info["displayName"]!.GetValue<string>(), Is.EqualTo("Test User"));
        Assert.That(info["profileUrl"]!.GetValue<string>(), Is.EqualTo("/profiles/1"));
        Assert.That(info["authProvider"]!.GetValue<string>(), Is.EqualTo("credentials"));
        Assert.That(info["roles"]!.AsArray()[0]!.GetValue<string>(), Is.EqualTo("Manager"));
    }

    [Test]
    public void RequiredRole_accepts_matching_role_and_Admin_session()
    {
        var auth = new IdentityChatAuth(new ChatFeature { RequireAuth = true, RequiredRole = "Manager" });

        Assert.That(auth.CheckAuth(CreateRequest(new AuthUserSession
        {
            IsAuthenticated = true,
            UserAuthName = "manager",
            Roles = ["Manager"],
        })).IsAuthenticated, Is.True);

        Assert.That(auth.CheckAuth(CreateRequest(new AuthUserSession
        {
            IsAuthenticated = true,
            UserAuthName = "admin",
            Roles = [RoleNames.Admin],
        })).IsAuthenticated, Is.True);
    }

    [Test]
    public void RequiredRole_rejects_session_without_role()
    {
        var auth = new IdentityChatAuth(new ChatFeature { RequireAuth = true, RequiredRole = "Manager" });
        var request = CreateRequest(new AuthUserSession
        {
            IsAuthenticated = true,
            UserAuthName = "user",
            Roles = ["Employee"],
        });

        Assert.That(auth.CheckAuth(request).IsAuthenticated, Is.False);
        Assert.That(auth.IsAdmin(request), Is.False);
    }

    [Test]
    public void Ignores_unauthenticated_ServiceStack_session()
    {
        var auth = new IdentityChatAuth(new ChatFeature { RequireAuth = true });
        var request = CreateRequest(new AuthUserSession
        {
            UserAuthName = "user@email.com",
            Roles = [RoleNames.Admin],
        });

        Assert.That(auth.GetUserName(request), Is.Null);
        Assert.That(auth.CheckAuth(request).IsAuthenticated, Is.False);
        Assert.That(auth.IsAdmin(request), Is.False);
    }
}
