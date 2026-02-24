using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Grpc;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Text.Pools;
using ServiceStack.Web;
using JwtConstants = Microsoft.IdentityModel.JsonWebTokens.JwtConstants;

namespace ServiceStack.Auth;

/// <summary>
/// Converts an MVC JwtBearer Cookie into a ServiceStack Session
/// </summary>
public class IdentityJwtAuthProvider<TUser,TRole,TKey> : 
    IdentityAuthProvider<TUser,TRole,TKey>, IIdentityJwtAuthProvider, IAuthWithRequest, IAuthResponseFilter
    where TUser : IdentityUser<TKey>, new()
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    public override string Type => "Bearer";
    public const string Name = AuthenticateService.JwtProvider;
    public const string Realm = "/auth/" + AuthenticateService.JwtProvider;

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
    
    public AuthenticationScheme? Scheme { get; set; }

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
    /// Register ConvertSessionToTokenService
    /// </summary>
    public bool IncludeConvertSessionToTokenService { get; set; }
    
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
    /// Invoked after Refresh Token is created
    /// </summary>
    public Action<IRequest, TUser>? OnRefreshTokenCreated { get; set; }

    /// <summary>
    /// Run custom filter after session is restored from a JWT Token
    /// </summary>
    public Action<IAuthSession, List<Claim>, IRequest>? OnSessionCreated { get; set; }

    /// <summary>
    /// Filter which claims are included in the JWT Token
    /// </summary>
    public Func<List<Claim>, List<Claim>> TokenClaimsFilter { get; set; } = DefaultTokenClaimsFilter;

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

        var schemeProvider = applicationServices.GetService<IAuthenticationSchemeProvider>();
        if (schemeProvider != null)
        {
            Scheme = schemeProvider.GetSchemeAsync(AuthenticationScheme).Result;
        }

        var optionsMonitor = applicationServices.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        Options = optionsMonitor.Get(AuthenticationScheme);
        
        var cookieOptions = applicationServices.GetService<IOptionsMonitor<CookiePolicyOptions>>()?.Get("");
        RequireSecureConnection = cookieOptions?.Secure != CookieSecurePolicy.None;

        var tokenParams = Options.TokenValidationParameters;
        Options.Events ??= new();
        Options.Events.OnMessageReceived = MessageReceivedAsync;
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
        
        if (ctx.Request is GrpcRequest)
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

            var jwtSession = CreateSessionFromClaims(req, principal);
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
        if (!string.IsNullOrEmpty(token) && 
            !(req.Items.TryGetValue(Keywords.Session, out var oSession) && oSession is IAuthSession { IsAuthenticated: true }))
        {
            var user = req.GetClaimsPrincipal();
            if (!user.IsAuthenticated())
            {
                user = new JwtSecurityTokenHandler().ValidateToken(token,
                    Options!.TokenValidationParameters, out SecurityToken validatedToken);
            }
            var session = CreateSessionFromClaims(req, user);
            req.SetItem(Keywords.Session, session);
        }
        return Task.CompletedTask;
    }

    public async Task MessageReceivedAsync(MessageReceivedContext ctx)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        ctx.Token = auth.StartsWith("Bearer ")
            ? auth.Substring("Bearer ".Length)
            : ctx.Request.Cookies.TryGetValue(Keywords.TokenCookie, out var cookieValue)
                ? cookieValue
                : null;

        var refreshToken = ctx.Request.Cookies.TryGetValue(Keywords.RefreshTokenCookie, out cookieValue)
            ? cookieValue
            : null;

        if (ctx.Token != null || refreshToken != null)
        {
            bool isValid = false;
            if (ctx.Token != null)
            {
                try
                {
                    var principal = new JwtSecurityTokenHandler().ValidateToken(ctx.Token,
                        Options!.TokenValidationParameters, out SecurityToken validatedToken);
                    return;
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("JWT Identity BearerToken '{0}...' failed: {1}{2}", 
                        ctx.Token.SafeSubstring(0,4), e.Message, refreshToken == null ? "" : ", trying Refresh Token...");
                }
            }

            if (refreshToken != null)
            {
                var req = ctx.Request.ToRequest(GetType().Name);
                try
                {
                    ctx.Token = await CreateAccessTokenFromRefreshTokenAsync(refreshToken, req).ConfigAwait();
                    ctx.Response.Cookies.Append(Keywords.TokenCookie, ctx.Token, new CookieOptions {
                        HttpOnly = true,
                        Secure = RequireSecureConnection,
                        Expires = DateTime.UtcNow.Add(ExpireTokensIn),
                    });
                }
                catch (Exception refreshEx)
                {
                    var errorMsg = string.Format("Failed to create AccessToken from RefreshToken '{0}...': {1}", 
                        refreshToken.SafeSubstring(0,4), refreshEx.Message);
                    Log.WarnFormat(errorMsg);
                    ctx.Fail(errorMsg);
                    ctx.HttpContext.Items[Keywords.ResponseStatus] = new ResponseStatus
                    {
                        ErrorCode = nameof(HttpStatusCode.Unauthorized),
                        Message = errorMsg,
                    };
                }
            }
        }
    }

    public virtual IAuthSession CreateSessionFromClaims(IRequest req, ClaimsPrincipal principal)
    {
        var claims = principal.Claims.ToList();
        var sessionId = claims.FirstOrDefault(x => x.Type == "jid")?.Value ?? HostContext.AppHost.CreateSessionId();
        var session = SessionFeature.CreateNewSession(req, sessionId);

        session.IsAuthenticated = true;
        session.AuthProvider = Name;
        session.FromToken = true;

        IdentityAuth.AuthApplication.PopulateSession(req, session, principal);

        OnSessionCreated?.Invoke(session, claims, req);

        HostContext.AppHost.OnSessionFilter(req, session, sessionId);
        return session;
    }

    public virtual async Task ExecuteAsync(AuthFilterContext authContext)
    {
        var session = authContext.Session;
        var authService = authContext.AuthService;

        var shouldIgnore = authContext.Request.Dto is IMeta meta && meta.Meta?.TryGetValue(Keywords.Ignore, out var ignore) == true && ignore == "jwt";
        var shouldReturnTokens = authContext.DidAuthenticate && !shouldIgnore;
        if (shouldReturnTokens && authContext.AuthResponse.BearerToken == null && session.IsAuthenticated)
        {
            if (authService.Request.AllowConnection(RequireSecureConnection))
            {
                var req = authContext.Request;
                var (user, principal) = await Manager.GetUserClaimsPrincipalByNameAsync(session.UserAuthName, authService.Request).ConfigAwait();
                var roles = authContext.Request.GetClaimsPrincipalRoles(principal);
                var bearerToken = CreateJwtBearerToken(user, principal, req);

                authContext.Session.UserAuthId = authContext.AuthResponse.UserId = user.Id.ToString();
                authContext.Session.Roles = roles.ToList();
                authContext.UserSource = user;

                authContext.AuthResponse.BearerToken = bearerToken; 
                var userRefreshToken = await CreateRefreshTokenAsync(user, authService.Request).ConfigAwait();
                if (userRefreshToken != null)
                {
                    authContext.AuthResponse.RefreshToken = userRefreshToken.RefreshToken;
                    authContext.AuthResponse.RefreshTokenExpiry = userRefreshToken.RefreshTokenExpiry;
                }
            }
        }
    }

    public async Task<List<Claim>> GetUserClaimsAsync(string userName, IRequest? req = null)
    {
        var (user, principal) = await Manager.GetUserClaimsPrincipalByNameAsync(userName, req).ConfigAwait();
        var claims = CreateUserClaims(user, principal);
        return claims;
    }

    public async Task<string> CreateBearerTokenAsync(string userName, IRequest? req = null)
    {
        var (user, principal) = await Manager.GetUserClaimsPrincipalByNameAsync(userName, req).ConfigAwait();
        return CreateJwtBearerToken(user, principal, req);
    }

    public async Task<UserJwtTokens> CreateBearerAndRefreshTokenAsync(string userName, IRequest? req = null)
    {
        var (user, principal) = await Manager.GetUserClaimsPrincipalByNameAsync(userName, req).ConfigAwait();
        var jwt = CreateJwtBearerToken(user, principal, req);
        if (user is IRequireRefreshToken hasRefreshToken && EnableRefreshToken)
        {
            if (hasRefreshToken.RefreshToken == null || hasRefreshToken.RefreshTokenExpiry == null || hasRefreshToken.RefreshTokenExpiry < DateTime.UtcNow)
            {
                hasRefreshToken = (await CreateRefreshTokenAsync(user, req).ConfigAwait())!;
            }
            return new(jwt, hasRefreshToken);
        }
        
        return new(jwt, null);
    }
    
    public string CreateJwtBearerToken(TUser user, ClaimsPrincipal principal, IRequest? req = null)
    {
        var userClaims = CreateUserClaims(user, principal);

        if (req != null)
        {
            var jti = ResolveJwtId?.Invoke(req);
            if (jti != null)
                userClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));

            OnTokenCreated?.Invoke(req, user, userClaims);
        }

        return CreateJwtBearerToken(userClaims, Audience, DateTime.UtcNow.Add(ExpireTokensIn));
    }
    
    public static List<Claim> DefaultTokenClaimsFilter(List<Claim> claims)
    {
        // filter out duplicate role and permission claims
        var existingRoles = new List<string>();
        var existingPerms = new List<string>();
        
        var to = new List<Claim>();
        foreach (var claim in claims)
        {
            if (claim.Type is JwtClaimTypes.Role or JwtClaimTypes.IdentityRole)
            {
                if (existingRoles.Contains(claim.Value))
                    continue;
                existingRoles.Add(claim.Value);
            }
            else if (claim.Type == JwtClaimTypes.Permission)
            {
                if (existingPerms.Contains(claim.Value))
                    continue;
                existingPerms.Add(claim.Value);
            }
            to.Add(claim);
        }
        
        return to;
    }

    public string CreateJwtBearerToken(List<Claim> claims, string audience, DateTime expires)
    {
        claims = TokenClaimsFilter(claims);
        
        var credentials = new SigningCredentials(TokenValidationParameters.IssuerSigningKey, HashAlgorithm);
        var securityToken = new JwtSecurityToken(
            issuer: TokenValidationParameters.ValidIssuer,
            audience: audience,
            expires: expires,
            claims: claims,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        return token;
    }

    public List<Claim> CreateUserClaims(TUser user, ClaimsPrincipal? principal = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()!),
            new(JwtClaimTypes.PreferredUserName, user.UserName ?? throw new ArgumentNullException(nameof(user.UserName))),
        };

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

        if (principal != null)
        {
            claims.AddRange(principal.Claims);
        }

        return claims;
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

    protected virtual async Task<IRequireRefreshToken?> CreateRefreshTokenAsync(TUser user, IRequest? req = null)
    {
#if NET8_0_OR_GREATER
        if (!EnableRefreshToken)
            return null;

        if (user is IRequireRefreshToken requireRefreshToken)
        {
            requireRefreshToken.RefreshToken = GenerateRefreshToken();
            requireRefreshToken.RefreshTokenExpiry = DateTime.UtcNow.Add(ExpireRefreshTokensIn);

            if (req != null)
            {
                OnRefreshTokenCreated?.Invoke(req, user);
            }

            async Task UpdateUser(DbContext dbContext)
            {
                var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);
                await dbUsers.Where(x => x.Id.Equals(user.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => ((IRequireRefreshToken)x).RefreshToken, requireRefreshToken.RefreshToken)
                        .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, requireRefreshToken.RefreshTokenExpiry)).ConfigAwait();
            }

            if (req == null)
            {
                var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(scope.ServiceProvider);
                await UpdateUser(dbContext).ConfigAwait();
            }
            else
            {
                await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(req.GetServiceProvider());
                await UpdateUser(dbContext).ConfigAwait();
            }

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
            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(req.GetServiceProvider());
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);

            await dbUsers.Where(x => ((IRequireRefreshToken)x).RefreshToken!.Equals(refreshToken))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshToken, null as string)
                    .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, null as DateTime?)).ConfigAwait();
        }
#endif
    }

    public async Task<string> CreateAccessTokenFromRefreshTokenAsync(string refreshToken, IRequest? req = null)
    {
        async Task<TUser> GetUser(DbSet<TUser> dbUsers)
        {
            var now = DateTime.UtcNow;
            var user = await dbUsers
                .Where(x => ((IRequireRefreshToken)x).RefreshToken == refreshToken)
                .SingleOrDefaultAsync();

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(req));

            var hasRefreshToken = (IRequireRefreshToken)user;
            if (hasRefreshToken.RefreshTokenExpiry == null || now > hasRefreshToken.RefreshTokenExpiry)
                throw HttpError.Forbidden(ErrorMessages.RefreshTokenInvalid.Localize(req));
        
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(req));
            return user;
        }
        
        async Task UpdateRefreshTokenExpiry(DbSet<TUser> dbUsers)
        {
#if NET8_0_OR_GREATER
            if (ExtendRefreshTokenExpiryAfterUsage != null)
            {
                var updatedDate = DateTime.UtcNow.Add(ExtendRefreshTokenExpiryAfterUsage.Value);
                await dbUsers.Where(x => ((IRequireRefreshToken)x).RefreshToken!.Equals(refreshToken))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => ((IRequireRefreshToken)x).RefreshTokenExpiry, updatedDate)).ConfigAwait();
            }
#endif
        }
        
        async Task<ClaimsPrincipal> Create(TUser? user, 
            UserManager<TUser> userManager, 
            RoleManager<TRole> roleManager,
            IOptions<IdentityOptions> options, 
            IRequest? httpReq = null)
        {
            if (user == null && httpReq != null)
            {
                var context = httpReq.TryResolve<IdentityAuthContext<TUser, TRole, TKey>>();
                var session = await httpReq.GetSessionAsync().ConfigAwait();
                if (HostContext.AssertPlugin<AuthFeature>().AuthSecretSession == session)
                    user = context.SessionToUserConverter(session);
            }

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(httpReq));

            var userFactory = new UserClaimsPrincipalFactory<TUser, TRole>(userManager, roleManager, options);
            var principal = await userFactory.CreateAsync(user).ConfigAwait();
            return principal;
        }

        if (req == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(scope.ServiceProvider);
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);

            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>();

            var user = await GetUser(dbUsers).ConfigAwait();
            var principal = await Create(user, userManager, roleManager, options).ConfigAwait();

            await UpdateRefreshTokenExpiry(dbUsers).ConfigAwait();
            return CreateJwtBearerToken(user, principal, req);
        }
        else
        {
            var services = req.GetServiceProvider();
            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(services);
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);

            var userManager = services.GetRequiredService<UserManager<TUser>>();
            var roleManager = services.GetRequiredService<RoleManager<TRole>>();
            var options = services.GetRequiredService<IOptions<IdentityOptions>>();
            
            var user = await GetUser(dbUsers).ConfigAwait();

            var principal = await Create(user, userManager, roleManager, options).ConfigAwait();
            await UpdateRefreshTokenExpiry(dbUsers).ConfigAwait();
            return CreateJwtBearerToken(user, principal, req);
        }
    }

    public async Task ResultFilterAsync(AuthResultContext authContext, CancellationToken token = default)
    {
        var addJwtCookie = authContext.Result.Cookies.All(x => x.Name != IdentityAuth.TokenCookie);
        var addRefreshCookie = authContext.Result.Cookies.All(x => x.Name != IdentityAuth.RefreshTokenCookie) && EnableRefreshToken;

        if (addJwtCookie || addRefreshCookie)
        {
            var (user, principal) = await Manager.GetUserClaimsPrincipalByNameAsync(authContext.Session.UserAuthName, authContext.Request).ConfigAwait();
            if (addJwtCookie)
            {
                var accessToken = CreateJwtBearerToken(user, principal, authContext.Request);
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
                var userRefreshToken = await CreateRefreshTokenAsync(user, authContext.Request).ConfigAwait();
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

[Authenticate]
[DefaultRequest(typeof(ConvertSessionToToken))]
public class ConvertSessionToTokenService(IIdentityJwtAuthProvider jwtAuthProvider) : Service
{
    public async Task<object> Any(ConvertSessionToToken request)
    {
        if (!Request.AllowConnection(jwtAuthProvider.RequireSecureConnection))
            throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(Request));

        if (Request.ResponseContentType.MatchesContentType(MimeTypes.Html))
            Request.ResponseContentType = MimeTypes.Json;

        var dto = new ConvertSessionToTokenResponse();
        var httpResult = new HttpResult(dto); 

        var token = Request.GetJwtToken();
        IAuthSession? session = null;
        UserJwtTokens? userTokens = null;
        var createFromSession = string.IsNullOrEmpty(token);
        if (!createFromSession)
        {
            userTokens = new(token, null);
        }
        else
        {
            session = await Request.GetSessionAsync().ConfigAwait();

            if (createFromSession)
                token = await jwtAuthProvider.CreateBearerTokenAsync(session.UserAuthName, Request).ConfigAwait();

            if (!request.PreserveSession)
            {
                if (session.Id != null)
                    await Request.RemoveSessionAsync(session.Id).ConfigAwait();

                jwtAuthProvider.DeleteCookiesOnJwtCookies.ForEach(name => httpResult.DeleteCookie(Request, name));
                
                if (jwtAuthProvider.EnableRefreshToken)
                {
                    userTokens = await jwtAuthProvider.CreateBearerAndRefreshTokenAsync(session.UserAuthName, Request).ConfigAwait();
                }
            }
            userTokens ??= new(token, null);
        }

        if (Request is GrpcRequest)
        {
            dto.AccessToken = token;
            dto.RefreshToken = userTokens.RefreshToken?.RefreshToken;
        }
        else
        {
            httpResult.AddCookie(Request,
                new Cookie(Keywords.TokenCookie, token, Cookies.RootPath) {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Expires = DateTime.UtcNow.Add(jwtAuthProvider.ExpireTokensIn),
                });
            
            if (userTokens.RefreshToken is { RefreshTokenExpiry: not null })
            {
                httpResult.AddCookie(Request,
                    new Cookie(Keywords.RefreshTokenCookie, userTokens.RefreshToken?.RefreshToken, Cookies.RootPath) {
                        HttpOnly = true,
                        Secure = Request.IsSecureConnection,
                        Expires = userTokens.RefreshToken!.RefreshTokenExpiry.Value,
                    });
            }
        }
        
        return httpResult;
    }
}

[DefaultRequest(typeof(GetAccessToken))]
public class GetAccessTokenIdentityService(IIdentityJwtAuthProvider jwtAuthProvider) : Service
{
    public async Task<object> Any(GetAccessToken request)
    {
        if (jwtAuthProvider.RequireSecureConnection && !Request.IsSecureConnection)
            throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(Request));

        var refreshTokenCookie = Request.Cookies.TryGetValue(Keywords.RefreshTokenCookie, out var refTok)
            ? refTok.Value
            : null; 

        var refreshToken = request.RefreshToken ?? refreshTokenCookie;
        if (refreshToken == null)
            throw HttpError.Forbidden(ErrorMessages.RefreshTokenInvalid.Localize(Request));
        
        var accessToken = await jwtAuthProvider.CreateAccessTokenFromRefreshTokenAsync(refreshToken, Request).ConfigAwait();

        var httpResult = new HttpResult(new GetAccessTokenResponse())
            .AddCookie(Request,
                new Cookie(Keywords.TokenCookie, accessToken, Cookies.RootPath) {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Expires = DateTime.UtcNow.Add(jwtAuthProvider.ExpireTokensIn),
                });
        
        return httpResult;
    }
}
