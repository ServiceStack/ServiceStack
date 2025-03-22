using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack.Host;
using ServiceStack.Html;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class IdentityBasicAuthProvider<TUser,TRole,TKey> : IdentityAuthProvider<TUser,TRole,TKey>, IAuthWithRequest, IIdentityBasicAuthProvider
    where TUser : IdentityUser<TKey>, new()
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    public new static string Name = AuthenticateService.BasicProvider;
    public new static string Realm = "/auth/" + AuthenticateService.BasicProvider;

    public override string Type => "Basic";

    public IdentityBasicAuthProvider()
    {
        this.Provider = Name;
        this.AuthRealm = Realm;

        Sort = -1;
        Label = "Basic Auth";
        FormLayout =
        [
            Input.For<Authenticate>(x => x.UserName, c =>
            {
                c.Label = "Email address";
                c.Required = true;
            }),
            Input.For<Authenticate>(x => x.Password, c =>
            {
                c.Type = "Password";
                c.Required = true;
            }),
        ];
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null) => false;

    public override Task<object?> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        return Task.FromResult(null as object);
    }

    public async Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var userPass = req.GetBasicAuthUserAndPassword();
        if (!string.IsNullOrEmpty(userPass?.Value))
        {
            var signInManager = req.TryResolve<SignInManager<TUser>>();
            var result = await signInManager.PasswordSignInAsync(userPass.Value.Key, userPass.Value.Value, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded && req.GetClaimsPrincipal() != null)
            {
                // After successful SignIn HttpContext.User is populated which is converted to session by AuthApplication provider
                await IdentityAuth.AuthApplication.PreAuthenticateAsync(req, res).ConfigAwait();
            }
        }
    }
}