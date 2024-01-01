#nullable enable

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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IIdentityJwtAuthProvider
{
    string? AuthenticationScheme { get; }
    JwtBearerOptions? Options { get; }
}

/// <summary>
/// Converts an MVC JwtBearer Cookie into a ServiceStack Session
/// </summary>
/// <typeparam name="TUser"></typeparam>
public class IdentityJwtAuthProvider<TUser,TKey> : IdentityAuthProvider<TUser,TKey>, IIdentityJwtAuthProvider, IAuthWithRequest, IAuthResponseFilter
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>
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
    /// How long should JWT Refresh Tokens be valid for. (default 365 days)
    /// </summary>
    public TimeSpan ExpireRefreshTokensIn { get; set; } = TimeSpan.FromDays(365);

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
    /// Get the last jti AutoId generated  
    /// </summary>
    public string LastJwtId() => Interlocked.Read(ref accessIdCounter).ToString();

    /// <summary>
    /// Change resolution for resolving unique jti id for Refresh Tokens
    /// </summary>
    public Func<IRequest, string>? ResolveRefreshJwtId { get; set; }

    /// <summary>
    /// Get the next AutoId for usage in jti JWT Refresh Tokens  
    /// </summary>
    public string NextRefreshJwtId() => Interlocked.Decrement(ref refreshIdCounter).ToString();

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

    private long refreshIdCounter;
    public string LastRefreshJwtId() => Interlocked.Read(ref refreshIdCounter).ToString();

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
    public Action<IRequest, string, List<Claim>>? OnRefreshTokenCreated { get; set; }

    /// <summary>
    /// Run custom filter after session is restored from a JWT Token
    /// </summary>
    public Action<IAuthSession, List<Claim>, IRequest>? OnSessionCreated { get; set; }

    public IdentityJwtAuthProvider(string? authenticationScheme = null)
        : base(null, Realm, Name)
    {
        AuthenticationScheme = authenticationScheme ?? JwtBearerDefaults.AuthenticationScheme;
        Options = new JwtBearerOptions();
        ResolveJwtId = _ => NextJwtId();
        ResolveRefreshJwtId = _ => NextRefreshJwtId();

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
            DateTime.UtcNow.Add(ExpireRefreshTokensIn),
            ctx.ReferrerUrl);
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
            DateTime.UtcNow.Add(ExpireRefreshTokensIn),
            ctx.ReferrerUrl);
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

    protected virtual bool EnableRefreshToken() => true;

    public async Task<(TUser, IEnumerable<string>)> GetUserAndRolesAsync(IServiceBase service, string email)
    {
        var userManager = service.TryResolve<UserManager<TUser>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            var session = await service.GetSessionAsync().ConfigAwait();
            if (HostContext.AssertPlugin<AuthFeature>().AuthSecretSession == session)
                user = IdentityAuth.Instance<TUser,TKey>()!.SessionToUserConverter(session);
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
                authContext.AuthResponse.RefreshToken = EnableRefreshToken()
                    ? CreateJwtRefreshToken(authService.Request, user.Id.ToString()!, ExpireRefreshTokensIn)
                    : null;
            }
        }
    }

    protected string? CreateJwtBearerToken(IRequest req, TUser user, IEnumerable<string>? roles = null)
    {
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()!),
            new(JwtClaimTypes.PreferredUserName, user.UserName),
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

    protected virtual string? CreateJwtRefreshToken(IRequest req, string userId, TimeSpan expireRefreshTokensIn)
    {
        List<Claim> claims = [
            new(JwtRegisteredClaimNames.Typ, "JWTR"),
            new(JwtRegisteredClaimNames.Sub, userId)
        ];

        var jti = ResolveRefreshJwtId?.Invoke(req);
        if (jti != null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));

        OnRefreshTokenCreated?.Invoke(req, userId, claims);

        var credentials = new SigningCredentials(TokenValidationParameters.IssuerSigningKey, HashAlgorithm);
        var securityToken = new JwtSecurityToken(
            issuer: TokenValidationParameters.ValidIssuer,
            audience: Audience,
            expires: DateTime.UtcNow.Add(expireRefreshTokensIn),
            claims: claims,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        return token;
    }

    public async Task ResultFilterAsync(AuthResultContext authContext, CancellationToken token = default)
    {
        if (authContext.Result.Cookies.All(x => x.Name != IdentityAuth.TokenCookie))
        {
            var (user, roles) = await GetUserAndRolesAsync(authContext.Service, authContext.Session.UserAuthName).ConfigAwait();
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

        if (authContext.Result.Cookies.All(x => x.Name != IdentityAuth.RefreshTokenCookie) && EnableRefreshToken())
        {
            var refreshToken = CreateJwtRefreshToken(authContext.Request, authContext.Session.Id, ExpireRefreshTokensIn);

            authContext.Result.AddCookie(authContext.Request,
                new Cookie(IdentityAuth.RefreshTokenCookie, refreshToken, Cookies.RootPath)
                {
                    HttpOnly = true,
                    Secure = authContext.Request.IsSecureConnection,
                    Expires = DateTime.UtcNow.Add(ExpireRefreshTokensIn),
                });
        }

        JwtUtils.NotifyJwtCookiesUsed(authContext.Result);
    }
}