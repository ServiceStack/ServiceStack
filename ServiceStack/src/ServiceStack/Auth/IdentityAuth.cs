#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IIdentityAuthContext
{
    Func<IAuthSession> SessionFactory { get; }
}

public interface IIdentityApplicationAuthProvider
{
    Task PreAuthenticateAsync(IRequest req, IResponse res);
    void PopulateSession(IRequest req, IAuthSession session, ClaimsPrincipal claimsPrincipal, string? source = null);
    Task PopulateSessionAsync(IRequest req, IAuthSession session, ClaimsPrincipal claimsPrincipal, string? source = null);
}

public interface IIdentityAuthContextManager
{
    Task<IList<Claim>> GetClaimsByIdAsync(string userId, IRequest? request = null);
    Task<IList<Claim>> GetClaimsByNameAsync(string userName, IRequest? request = null);
    Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(string userId, IRequest? request = null);
}

public interface IIdentityCredentialsAuthProvider
{
    bool LockoutOnFailure { get; set; }
}

public interface IIdentityBasicAuthProvider
{
}

public class UserJwtTokens(string BearerToken, IRequireRefreshToken? RefreshToken)
{
    public string BearerToken { get; } = BearerToken;
    public IRequireRefreshToken? RefreshToken { get; } = RefreshToken;

    public void Deconstruct(out string BearerToken, out IRequireRefreshToken? RefreshToken)
    {
        BearerToken = this.BearerToken;
        RefreshToken = this.RefreshToken;
    }
}

public interface IIdentityJwtAuthProvider
{
    string? AuthenticationScheme { get; }
    List<string> DeleteCookiesOnJwtCookies { get; }
    // JwtBearerOptions? Options { get; }
    bool EnableRefreshToken { get; }
    bool RequireSecureConnection { get; }
    string Audience { get; }
    TimeSpan ExpireTokensIn { get; }
    Task<List<Claim>> GetUserClaimsAsync(string userName, IRequest? req = null);
    string CreateJwtBearerToken(List<Claim> claims, string audience, DateTime expires);
    Task<string> CreateBearerTokenAsync(string userName, IRequest? req = null);
    Task<UserJwtTokens> CreateBearerAndRefreshTokenAsync(string userName, IRequest? req = null);
    Task<string> CreateAccessTokenFromRefreshTokenAsync(string refreshToken, IRequest? req = null);
}

