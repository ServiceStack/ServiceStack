using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Text.Pools;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IIdentityJwtAuthProvider
{
    string? AuthenticationScheme { get; }
    JwtBearerOptions? Options { get; }
    bool RequireSecureConnection { get; }
    TimeSpan ExpireTokensIn { get; }
    Task<string> CreateAccessTokenFromRefreshTokenAsync(string refreshToken, IRequest request);
}

/// <summary>
/// Converts an MVC JwtBearer Cookie into a ServiceStack Session
/// </summary>
public class IdentityJwtAuthProvider<TUser,TKey> : IdentityAuthProvider<TUser,TKey>, IIdentityJwtAuthProvider, IAuthWithRequest, IAuthResponseFilter
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    public override string Type => "Bearer";
    public const string Name = "identity";
    public const string Realm = "/auth/identity";

    /// <summary>
    /// Default Issuer to use if unspecified
    /// </summary>
    public string DefaultIssuer { get; set; } = "ssjwt";

    /// <summary>
    /// Which Hash Algorithm should be used to sign the JWT Token. (default HS256)
    /// </summary>
    public string HashAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256;

    /// <summary>
    /// Which JWT Authentication Scheme configuration to use (default Bearer)
    /// </summary>
    public string AuthenticationScheme { get; }

    /// <summary>
    /// The JWT Bearer Options to use (default populated from AuthenticationScheme JwtBearerOptions)
    /// </summary>
    public JwtBearerOptions Options { get; set; }

    /// <summary>
    /// The JwtBearerOptions TokenValidationParameters short-hand
    /// </summary>
    public TokenValidationParameters TokenValidationParameters
    {
        get => Options.TokenValidationParameters;
        set => Options.TokenValidationParameters = value;
    }

    /// <summary>
    /// How long should JWT Tokens be valid for. (default 14 days)
    /// </summary>
    public TimeSpan ExpireTokensIn { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// How long should JWT Refresh Tokens be valid for. (default 90 days)
    /// </summary>
    public TimeSpan ExpireRefreshTokensIn { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Convenient overload to initialize ExpireTokensIn with an Integer
    /// </summary>
    public int ExpireTokensInDays
    {
        set
        {
            if (value > 0)
                ExpireTokensIn = TimeSpan.FromDays(value);
        }
    }

    /// <summary>
    /// Whether to only allow access via Bearer Token from a secure connection (default true)
    /// </summary>
    public bool RequireSecureConnection { get; set; } = true;

    /// <summary>
    /// Change resolution for resolving unique jti id for Access Tokens
    /// </summary>
    public Func<IRequest, string>? ResolveJwtId { get; set; }

    /// <summary>
    /// Get the next AutoId for usage in jti JWT Access Tokens  
    /// </summary>
    public string NextJwtId() => Interlocked.Increment(ref accessIdCounter).ToString();
    private long accessIdCounter;

    /// <summary>
    /// Whether to enable JWT Refresh Tokens (default TUser : IRequireRefreshToken) 
    /// </summary>
    public bool EnableRefreshToken { get; set; }

    /// <summary>
    /// Remove Auth Cookies on Authentication
    /// </summary>
    public List<string> DeleteCookiesOnJwtCookies { get; set; } = [".AspNetCore.Identity.Application"];

    /// <summary>
    /// Register GetAccessToken Service to enable Refresh Tokens
    /// </summary>
    public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new();
    
    /// <summary>
    /// Return Valid Audiences in comma-delimited string
    /// </summary>
    public string Audience
    {
        get
        {
            var audiences = (TokenValidationParameters.ValidAudiences ?? TypeConstants.EmptyStringArray).ToList();
            return audiences.Count > 0
                ? string.Join(',', audiences)
                : TokenValidationParameters.ValidAudience;
        }
    }

    public List<(string fieldName, string claimType)> MapIdentityUserToClaims { get; set; } =
    [
        new(nameof(ClaimTypes.Surname), JwtClaimTypes.FamilyName),
        new(nameof(JwtClaimTypes.Email), JwtClaimTypes.Email),
        new(nameof(JwtClaimTypes.GivenName), JwtClaimTypes.GivenName),
        new(nameof(JwtClaimTypes.FamilyName), JwtClaimTypes.FamilyName),
        new(nameof(JwtClaimTypes.Picture), JwtClaimTypes.Picture),
        new(nameof(JwtClaimTypes.Locale), JwtClaimTypes.Locale),
        new(nameof(JwtClaimTypes.WebSite), JwtClaimTypes.WebSite),
        new(nameof(JwtClaimTypes.NickName), JwtClaimTypes.NickName),
        new(nameof(JwtClaimTypes.EmailVerified), JwtClaimTypes.EmailVerified),
        new(nameof(AuthUserSession.FirstName), JwtClaimTypes.GivenName),
        new(nameof(AuthUserSession.LastName), JwtClaimTypes.FamilyName),
        new(nameof(AuthUserSession.DisplayName), JwtClaimTypes.Name),
        new(nameof(AuthUserSession.ProfileUrl), JwtClaimTypes.Picture),
        new(nameof(AuthUserSession.UserAuthName), JwtClaimTypes.PreferredUserName)
    ];

    public List<string> NameClaimFieldNames { get; set; } =
    [
        nameof(UserAuth.DisplayName),
        "Name",
        "FullName",
        nameof(JwtClaimTypes.GivenName),
        nameof(AuthUserSession.FirstName)
    ];

    /// <summary>
    /// Customize which claims are included in the JWT Token
    /// </summary>
    public Action<IRequest, TUser, List<Claim>>? OnTokenCreated { get; set; }

    /// <summary>
    /// Customize which claims are included in the JWT Refresh Token
    /// </summary>
    public Action<IRequest, TUser>? OnRefreshTokenCreated { get; set; }

    /// <summary>
    /// Run custom filter after session is restored from a JWT Token
    /// </summary>
    public Action<IAuthSession, List<Claim>, IRequest>? OnSessionCreated { get; set; }

#if NET8_0_OR_GREATER    
    /// <summary>
    /// Whether to invalidate Refresh Tokens on Logout (default true)
    /// </summary>
    public bool InvalidateRefreshTokenOnLogout { get; set; } = true;

    /// <summary>
    /// How long to extend the expiry of Refresh Tokens after usage (default None) 
    /// </summary>
    public TimeSpan? ExtendRefreshTokenExpiryAfterUsage { get; set; }
#endif
    
    public IdentityJwtAuthProvider(string? authenticationScheme = null)
        : base(null, Realm, Name)
    {
        AuthenticationScheme = authenticationScheme ?? JwtBearerDefaults.AuthenticationScheme;
        Options = new JwtBearerOptions();
        ResolveJwtId = _ => NextJwtId();
        EnableRefreshToken = typeof(TUser).HasInterface(typeof(IRequireRefreshToken));
            
        Label = "JWT";
        FormLayout = [
            new InputInfo(nameof(IHasBearerToken.BearerToken), Html.Input.Types.Textarea)
            {
                Label = "JWT",
                Placeholder = "JWT Bearer Token",
                Required = true,
            }
        ];
    }

    public override void Register(IAppHost appHost, AuthFeature feature)
    {
        base.Register(appHost, feature);
        var applicationServices = appHost.GetApplicationServices();

        var optionsMonitor = applicationServices.Resolve<IOptionsMonitor<JwtBearerOptions>>();
        Options = optionsMonitor.Get(AuthenticationScheme);

        var cookieOptions = applicationServices.TryResolve<IOptionsMonitor<CookiePolicyOptions>>()?.Get("");
        RequireSecureConnection = cookieOptions?.Secure != CookieSecurePolicy.None;

        var tokenParams = Options.TokenValidationParameters;
        tokenParams.ValidIssuer ??= DefaultIssuer;
        tokenParams.IssuerSigningKey ??= new SymmetricSecurityKey(AesUtils.CreateKey());

        feature.AuthResponseDecorator = AuthenticateResponseDecorator;
        feature.RegisterResponseDecorator = RegisterResponseDecorator;
        feature.OnLogoutAsync.Add(OnLogoutAsync);
    }

    public object AuthenticateResponseDecorator(AuthFilterContext ctx)
    {
        var req = ctx.AuthService.Request;
        if (req.IsInProcessRequest())
            return ctx.AuthResponse;

        if (ctx.AuthResponse.BearerToken == null)
            return ctx.AuthResponse;

        req.RemoveSession();

        var httpResult = ctx.AuthResponse.ToTokenCookiesHttpResult(req,
            IdentityAuth.TokenCookie,
            DateTime.UtcNow.Add(ExpireTokensIn),
            IdentityAuth.RefreshTokenCookie,
            ctx.ReferrerUrl);
        
        DeleteCookiesOnJwtCookies.ForEach(name => httpResult.DeleteCookie(req, name));
        return httpResult;
    }

    public object RegisterResponseDecorator(RegisterFilterContext ctx)
    {
        var req = ctx.Request;
        if (ctx.RegisterResponse.BearerToken == null)
            return ctx.RegisterResponse;

        var httpResult = ctx.RegisterResponse.ToTokenCookiesHttpResult(req,
            IdentityAuth.TokenCookie,
            DateTime.UtcNow.Add(ExpireTokensIn),
            IdentityAuth.RefreshTokenCookie,
            ctx.ReferrerUrl);
        
        DeleteCookiesOnJwtCookies.ForEach(name => httpResult.DeleteCookie(req, name));
        return httpResult;
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null)
    {
        return session is { FromToken: true, IsAuthenticated: true };
    }

    public override Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request,
        CancellationToken token = new())
    {
        // only allow verification of token
        if (!string.IsNullOrEmpty(request.Password) && string.IsNullOrEmpty(request.UserName))
        {
            var req = authService.Request;
            var bearerToken = request.Password;

            var principal = new JwtSecurityTokenHandler().ValidateToken(bearerToken,
                Options!.TokenValidationParameters, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var claims = jwtToken.Claims.ToList();

            var jwtSession = CreateSessionFromClaims(req, claims);
            var to = jwtSession.ConvertTo<AuthenticateResponse>();
            to.UserId = jwtSession.UserAuthId;
            return (to as object).InTask();
        }

        throw new NotImplementedException("JWT Authenticate() should not be called directly");
    }

    /// <summary>
    /// Populate ServiceStack Session from JWT
    /// </summary>
    public Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var token = req.GetJwtToken();
        if (!string.IsNullOrEmpty(token))
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token,
                Options!.TokenValidationParameters, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var claims = jwtToken.Claims.ToList();

            var session = CreateSessionFromClaims(req, claims);
            req.Items[Keywords.Session] = session;
        }
        return Task.CompletedTask;
     }

    public virtual IAuthSession CreateSessionFromClaims(IRequest req, List<Claim> claims)
    {
        var sessionId = claims.FirstOrDefault(x => x.Type == "jid")?.Value ?? HostContext.AppHost.CreateSessionId();
        var session = SessionFeature.CreateNewSession(req, sessionId);

        session.IsAuthenticated = true;
        session.AuthProvider = Name;
        session.FromToken = true;

        var claimMap = new List<KeyValuePair<string, string>>();
        claims.Each(x => claimMap.Add(new(x.Type, x.Value)));
        session.PopulateFromMap(claimMap);

        OnSessionCreated?.Invoke(session, claims, req);

        HostContext.AppHost.OnSessionFilter(req, session, sessionId);
        return session;
    }

    public async Task<(TUser, IEnumerable<string>)> GetUserAndRolesAsync(IServiceBase service, string email)
    {
        var userManager = service.TryResolve<UserManager<TUser>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            var session = await service.GetSessionAsync().ConfigAwait();
            if (HostContext.AssertPlugin<AuthFeature>().AuthSecretSession == session)
                user = Context.SessionToUserConverter(session);
        }

        if (user == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(service.Request));

        var roles = await userManager.GetRolesAsync(user);
        return (user, roles);
    }

    public virtual async Task ExecuteAsync(AuthFilterContext authContext)
    {
        var session = authContext.Session;
        var authService = authContext.AuthService;

        var shouldReturnTokens = authContext.DidAuthenticate;
        if (shouldReturnTokens && authContext.AuthResponse.BearerToken == null && session.IsAuthenticated)
        {
            if (authService.Request.AllowConnection(RequireSecureConnection))
            {
                var (user, roles) = await GetUserAndRolesAsync(authService, session.UserAuthName).ConfigAwait();

                authContext.Session.UserAuthId = authContext.AuthResponse.UserId = user.Id.ToString();
                authContext.Session.Roles = roles.ToList();
                authContext.UserSource = user;

                authContext.AuthResponse.BearerToken = CreateJwtBearerToken(authContext.AuthService.Request, user, authContext.Session.Roles);
                var userRefreshToken = await CreateRefreshTokenAsync(authService.Request, user).ConfigAwait();
                if (userRefreshToken != null)
                {
                    authContext.AuthResponse.RefreshToken = userRefreshToken.RefreshToken;
                    authContext.AuthResponse.RefreshTokenExpiry = userRefreshToken.RefreshTokenExpiry;
                }
            }
        }
    }

    protected string CreateJwtBearerToken(IRequest req, TUser user, IEnumerable<string>? roles = null)
    {
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()!),
            new(JwtClaimTypes.PreferredUserName, user.UserName ?? throw new ArgumentNullException(nameof(user.UserName))),
        };

        var jti = ResolveJwtId?.Invoke(req);
        if (jti != null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));

        if (NameClaimFieldNames.Count > 0 || MapIdentityUserToClaims.Count > 0)
        {
            var existingClaimTypes = claims.Select(x => x.Type).ToSet();
            var nameClaimType = TokenValidationParameters.NameClaimType ?? ClaimTypes.Name;
            var userProps = new Dictionary<string, object?>(user.ToObjectDictionary(), StringComparer.OrdinalIgnoreCase);

            foreach (var fieldName in NameClaimFieldNames)
            {
                if (!userProps.TryGetValue(fieldName, out var fieldValue))
                    continue;
                var valueStr = fieldValue?.ToString();
                if (valueStr == null)
                    continue;

                claims.Add(new Claim(nameClaimType, valueStr));
                existingClaimTypes.Add(nameClaimType);
            }

            foreach (var (fieldName, claimType) in MapIdentityUserToClaims)
            {
                if (existingClaimTypes.Contains(claimType))
                    continue;
                if (!userProps.TryGetValue(fieldName, out var fieldValue))
                    continue;
                var valueStr = fieldValue?.ToString();
                if (valueStr == null)
                    continue;
                claims.Add(new Claim(claimType, valueStr));
                existingClaimTypes.Add(claimType);
            }
        }

        if (roles != null)
        {
            var roleClaim = TokenValidationParameters.RoleClaimType ?? ClaimTypes.Role;
            foreach (var role in roles)
            {
                claims.Add(new Claim(roleClaim, role));
            }
        }

        OnTokenCreated?.Invoke(req, user, claims);

        var credentials = new SigningCredentials(TokenValidationParameters.IssuerSigningKey, HashAlgorithm);
        var securityToken = new JwtSecurityToken(
            issuer: TokenValidationParameters.ValidIssuer,
            audience: Audience,
            expires: DateTime.UtcNow.Add(ExpireTokensIn),
            claims: claims,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        return token;
    }
    
    public Func<string> GenerateRefreshToken { get; set; } = DefaultGenerateRefreshToken;

    public static string DefaultGenerateRefreshToken()
    {
        const int bufferSize = 64;
        var buf = BufferPool.GetBuffer(bufferSize);
        try
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(buf, 0, bufferSize);
            return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(buf, 0, bufferSize);
        }
        finally
        {
            BufferPool.ReleaseBufferToPool(ref buf);
        }
    }

    protected virtual async Task<IRequireRefreshToken?> CreateRefreshTokenAsync(IRequest req, TUser user)
    {
#if NET8_0_OR_GREATER
        if (!EnableRefreshToken)
            return null;

        if (user is IRequireRefreshToken requireRefreshToken)
        {
            requireRefreshToken.RefreshToken = GenerateRefreshToken();
            requireRefreshToken.RefreshTokenExpiry = DateTime.UtcNow.Add(ExpireRefreshTokensIn);

            OnRefreshTokenCreated?.Invoke(req, user);

            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(req);
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);
            await dbUsers.Where(x => x.Id.Equals(user.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshToken, requireRefreshToken.RefreshToken)
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, requireRefreshToken.RefreshTokenExpiry)).ConfigAwait();
            
            return requireRefreshToken;
        }
#else
        throw new NotSupportedException("IRequireRefreshToken requires .NET 8.0+");
#endif
        return null;
    }

    public async Task OnLogoutAsync(IRequest req)
    {
#if NET8_0_OR_GREATER
        var refreshToken = req.GetJwtRefreshToken();
        if (InvalidateRefreshTokenOnLogout && refreshToken != null)
        {
            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(req);
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);

            await dbUsers.Where(x => ((IRequireRefreshToken)x).RefreshToken!.Equals(refreshToken))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshToken, null as string)
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, null as DateTime?)).ConfigAwait();
        }
#endif
    }

    public async Task<string> CreateAccessTokenFromRefreshTokenAsync(string refreshToken, IRequest request)
    {
        await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(request);
        var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);

        var now = DateTime.UtcNow;

        var user = await dbUsers
            .Where(x => ((IRequireRefreshToken)x).RefreshToken == refreshToken)
            .SingleOrDefaultAsync();

        if (user == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

        var hasRefreshToken = (IRequireRefreshToken)user;
        if (hasRefreshToken.RefreshTokenExpiry == null || now > hasRefreshToken.RefreshTokenExpiry)
            throw HttpError.Forbidden(ErrorMessages.RefreshTokenInvalid.Localize(request));
        
        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(request));

        var userManager = request.TryResolve<UserManager<TUser>>();
        var roles = await userManager.GetRolesAsync(user);

#if NET8_0_OR_GREATER        
        if (ExtendRefreshTokenExpiryAfterUsage != null)
        {
            var updatedDate = DateTime.UtcNow.Add(ExtendRefreshTokenExpiryAfterUsage.Value);
            await dbUsers.Where(x => ((IRequireRefreshToken)x).RefreshToken!.Equals(refreshToken))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, updatedDate)).ConfigAwait();
        }
#endif
        
        var jwt = CreateJwtBearerToken(request, user, roles);
        return jwt;
    }

    public async Task ResultFilterAsync(AuthResultContext authContext, CancellationToken token = default)
    {
        var addJwtCookie = authContext.Result.Cookies.All(x => x.Name != IdentityAuth.TokenCookie);
        var addRefreshCookie = authContext.Result.Cookies.All(x => x.Name != IdentityAuth.RefreshTokenCookie) && EnableRefreshToken;

        if (addJwtCookie || addRefreshCookie)
        {
            var (user, roles) = await GetUserAndRolesAsync(authContext.Service, authContext.Session.UserAuthName).ConfigAwait();
            if (addJwtCookie)
            {
                var accessToken = CreateJwtBearerToken(authContext.Request, user, roles);
                await authContext.Request.RemoveSessionAsync(authContext.Session.Id, token);

                authContext.Result.AddCookie(authContext.Request,
                    new Cookie(IdentityAuth.TokenCookie, accessToken, Cookies.RootPath)
                    {
                        HttpOnly = true,
                        Secure = authContext.Request.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(ExpireTokensIn),
                    });
            }

            if (addRefreshCookie)
            {
                var userRefreshToken = await CreateRefreshTokenAsync(authContext.Request, user).ConfigAwait();
                if (userRefreshToken?.RefreshTokenExpiry != null)
                {
                    authContext.Result.AddCookie(authContext.Request,
                        new Cookie(IdentityAuth.RefreshTokenCookie, userRefreshToken.RefreshToken, Cookies.RootPath)
                        {
                            HttpOnly = true,
                            Secure = authContext.Request.IsSecureConnection,
                            Expires = userRefreshToken.RefreshTokenExpiry.Value,
                        });
                }
            }

            DeleteCookiesOnJwtCookies.ForEach(name => 
                authContext.Result.DeleteCookie(authContext.Request, name));
        }

        JwtUtils.NotifyJwtCookiesUsed(authContext.Result);
    }
}

[DefaultRequest(typeof(GetAccessToken))]
public class GetAccessTokenIdentityService : Service
{
    public async Task<object> Any(GetAccessToken request)
    {
        var jwtProvider = HostContext.AssertPlugin<AuthFeature>().AuthProviders
            .FirstOrDefault(x => x is IIdentityJwtAuthProvider) as IIdentityJwtAuthProvider
            ?? throw new NotSupportedException("IdentityJwtAuthProvider was not configured");
        
        if (jwtProvider.RequireSecureConnection && !Request.IsSecureConnection)
            throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(Request));

        var refreshTokenCookie = Request.Cookies.TryGetValue(Keywords.RefreshTokenCookie, out var refTok)
            ? refTok.Value
            : null; 

        var refreshToken = request.RefreshToken ?? refreshTokenCookie;
        if (refreshToken == null)
            throw HttpError.Forbidden(ErrorMessages.RefreshTokenInvalid.Localize(Request));
        
        var accessToken = await jwtProvider.CreateAccessTokenFromRefreshTokenAsync(refreshToken, Request).ConfigAwait();

        var httpResult = new HttpResult(new GetAccessTokenResponse())
            .AddCookie(Request,
                new Cookie(Keywords.TokenCookie, accessToken, Cookies.RootPath) {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Expires = DateTime.UtcNow.Add(jwtProvider.ExpireTokensIn),
                });
        
        return httpResult;
    }
}
