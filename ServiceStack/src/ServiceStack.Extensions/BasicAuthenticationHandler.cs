using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceStack;

public static class BasicAuthenticationHandler
{
    public const string Scheme = "basic";

    public static AuthenticationBuilder AddBasicAuth<TUser>(this AuthenticationBuilder builder)
        where TUser : IdentityUser, new() => builder.AddBasicAuth<TUser,string>();
    
    public static AuthenticationBuilder AddBasicAuth<TUser, TKey>(this AuthenticationBuilder builder)
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey>, new()
    {
        return builder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler<TUser, TKey>>(Scheme, null);
    }
}

public class BasicAuthenticationHandler<TUser> : BasicAuthenticationHandler<TUser, string>
    where TUser : IdentityUser<string>
{
#if NET6_0
    public BasicAuthenticationHandler(
        SignInManager<TUser> signInManager,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(signInManager, options, logger, encoder, clock) {}
#else
    public BasicAuthenticationHandler(
        SignInManager<TUser> signInManager,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(signInManager, options, logger, encoder) {}
#endif
}

public class BasicAuthenticationHandler<TUser, TKey>
#if NET6_0
    (SignInManager<TUser> signInManager,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
#else
    (SignInManager<TUser> signInManager,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
#endif
    where TUser : IdentityUser<TKey> 
    where TKey : IEquatable<TKey>
{  
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()  
    {  
        if (!Request.Headers.TryGetValue(HttpHeaders.Authorization, out var auth))
            return AuthenticateResult.NoResult();

        var authStr = auth.ToString();
        if (string.IsNullOrWhiteSpace(authStr) || authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        if (!AuthenticationHeaderValue.TryParse(authStr, out var authHeader)
            || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(authHeader.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        string credentials;
        try
        {
            var bytes = Convert.FromBase64String(authHeader.Parameter);
            credentials = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Base64-encoded credentials.");
        }

        var username = credentials.LeftPart(':');  
        var password = credentials.RightPart(':');

        var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
            return AuthenticateResult.Fail(result.ToString());
  
        var ticket = new AuthenticationTicket(Request.HttpContext.User, Scheme.Name);  
        return AuthenticateResult.Success(ticket);  
    }  
}
