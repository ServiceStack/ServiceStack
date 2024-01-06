using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Enable the authentication feature and configure the AuthService.
/// </summary>
public class AuthFeature : IPlugin, IPostInitPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Auth;
    //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
    public Regex ValidUserNameRegEx = AuthFeatureExtensions.ValidUserNameRegEx;

    public Func<string, bool> IsValidUsernameFn { get; set; }

    /// <summary>
    /// Fired before any [Authenticate] or [Required*] Auth Attribute is validated.
    /// Return non-null IHttpResult to write to response and short-circuit request.
    /// </summary>
    public Func<IRequest, IHttpResult> OnAuthenticateValidate { get; set; }

    /// <summary>
    /// Custom Validation Function in AuthenticateService 
    /// </summary>
    public ValidateFn ValidateFn { get; set; }

    public Action<IRequest, string> ValidateRedirectLinks { get; set; } = NoExternalRedirects;

    public static void AllowAllRedirects(IRequest req, string redirect) {}
    public static void NoExternalRedirects(IRequest req, string redirect)
    {
        redirect = redirect?.Trim();
        if (string.IsNullOrEmpty(redirect))
            return;

        if (redirect.StartsWith("//") || redirect.Contains("://"))
        {
            if (redirect.StartsWith(req.GetBaseUrl()))
                return;
                
            throw new ArgumentException(ErrorMessages.NoExternalRedirects, Keywords.Continue);
        }
    }

    public Func<IAuthSession> SessionFactory { get; set; }
    public Type SessionType { get; }
        
    private IAuthProvider[] authProviders;
    public IAuthProvider[] AuthProviders => authProviders;

    public Dictionary<Type, string[]> ServiceRoutes { get; set; }
    public Dictionary<string, string> ServiceRoutesVerbs { get; set; }

    public List<IPlugin> RegisterPlugins { get; set; } = [new SessionFeature()];

    public bool HasSessionFeature => RegisterPlugins.Any(x => x is SessionFeature);

    public List<IAuthEvents> AuthEvents { get; set; } = [];

    /// <summary>
    /// Invoked before AuthFeature is registered
    /// </summary>
    public List<Action<AuthFeature>> OnBeforeInit { get; set; } = [];

    /// <summary>
    /// Invoked after AuthFeature is registered
    /// </summary>
    public List<Action<AuthFeature>> OnAfterInit { get; set; } = [];
        
    /// <summary>
    /// Login path to redirect to
    /// </summary>
    public string HtmlRedirect { get; set; }

    /// <summary>
    /// Redirect path to when Access by Authenticated User is Denied
    /// </summary>
    public string HtmlRedirectAccessDenied { get; set; }
        
    /// <summary>
    /// What queryString param to capture redirect param on
    /// </summary>
    public string HtmlRedirectReturnParam { get; set; } = LocalizedStrings.Redirect;
        
    /// <summary>
    /// Redirect path to when Authenticated User requires 2FA
    /// </summary>
    public string HtmlRedirectLoginWith2Fa { get; set; }
        
    /// <summary>
    /// Redirect path to when User is Locked out
    /// </summary>
    public string HtmlRedirectLockout { get; set; }

    /// <summary>
    /// Whether to only capture return path or absolute URL (default)
    /// </summary>
    public bool HtmlRedirectReturnPathOnly { get; set; }

    /// <summary>
    /// Where users should redirect to after logging out
    /// </summary>
    public string HtmlLogoutRedirect { get; set; }

    public bool IncludeAuthMetadataProvider { get; set; } = true;

    public bool ValidateUniqueEmails { get; set; } = true;

    public bool ValidateUniqueUserNames { get; set; }

    public bool DeleteSessionCookiesOnLogout { get; set; } = true;

    public bool GenerateNewSessionCookiesOnAuthentication { get; set; } = true;
        
    /// <summary>
    /// Whether to Create Digest Auth MD5 Hash when Creating/Updating Users.
    /// Defaults to only creating Digest Auth when DigestAuthProvider is registered.
    /// </summary>
    public bool CreateDigestAuthHashes { get; set; }

    /// <summary>
    /// Should UserName or Emails be saved in AuthRepository in LowerCase
    /// </summary>
    public bool SaveUserNamesInLowerCase { get; set; }

    public TimeSpan? SessionExpiry { get; set; }

    public TimeSpan? PermanentSessionExpiry { get; set; }

    public int? MaxLoginAttempts { get; set; }

    public bool IncludeRolesInAuthenticateResponse { get; set; } = true;

    public bool IncludeOAuthTokensInAuthenticateResponse { get; set; }

    public bool IncludeDefaultLogin { get; set; } = true;

    public ImagesHandler ProfileImages { get; set; } = new("/auth-profiles", Svg.GetStaticContent(Svg.Icons.DefaultProfile)); 

    /// <summary>
    /// UI Layout for Authentication
    /// </summary>
    public List<InputInfo> FormLayout { get; set; } =
    [
        Input.For<Authenticate>(x => x.provider, x =>
        {
            x.Type = Input.Types.Select;
            x.Label = "";
        }),
        Input.For<Authenticate>(x => x.UserName, x => x.Label = "Email"),
        Input.For<Authenticate>(x => x.Password, x => x.Type = Input.Types.Password),
        Input.For<Authenticate>(x => x.RememberMe)
    ];

    public MetaAuthProvider AdminAuthSecretInfo { get; set; } = new() {
        Name = Keywords.AuthSecret,
        Type = Keywords.AuthSecret,
        Label = "Auth Secret",
        FormLayout =
        [
            new InputInfo(Keywords.AuthSecret, Input.Types.Password)
            {
                Label = "Auth Secret",
                Placeholder = "Admin Auth Secret",
                Required = true,
            }
        ]
    };

    /// <summary>
    /// Allow or deny all GET Authenticate Requests
    /// </summary>
    public Func<IRequest, bool> AllowGetAuthenticateRequests { get; set; } = DefaultAllowGetAuthenticateRequests;

    public static bool DefaultAllowGetAuthenticateRequests(IRequest req)
    {
        var provider = (req.Dto as Authenticate)?.provider;
            
        if (string.IsNullOrEmpty(provider) ||  // Allows empty /auth requests to check if Authenticated                 
            AuthenticateService.LogoutAction.EqualsIgnoreCase(provider)) // allows /auth/logout
            return true;
                   
        var authProvider = AuthenticateService.GetAuthProvider(provider);
        return authProvider  == null ||        // Unknown provider thrown in AuthService 
               authProvider is IOAuthProvider; // Allow all OAuth Providers by default 
    }

    public Func<AuthFilterContext, object> AuthResponseDecorator { get; set; }
    public Func<RegisterFilterContext, object> RegisterResponseDecorator { get; set; }

    public bool IncludeAssignRoleServices
    {
        set
        {
            if (!value)
            {
                (from registerService in ServiceRoutes
                        where registerService.Key == typeof(AssignRolesService)
                              || registerService.Key == typeof(UnAssignRolesService)
                        select registerService.Key).ToList()
                    .ForEach(x => ServiceRoutes.Remove(x));
            }
        }
    }

    public bool IncludeRegistrationService
    {
        set
        {
            if (value)
            {
                if (!RegisterPlugins.Any(x => x is RegistrationFeature))
                {
                    RegisterPlugins.Add(new RegistrationFeature());
                }
            }
        }
    }
        
    [Obsolete("The /authenticate alias routes are no longer added by default")]
    public AuthFeature RemoveAuthenticateAliasRoutes()
    {
        ServiceRoutes[typeof(AuthenticateService)] =
        [
            "/" + LocalizedStrings.Auth.Localize(),
            "/" + LocalizedStrings.Auth.Localize() + "/{provider}"
        ];
        return this;
    }

    /// <summary>
    /// Add /authenticate and /authenticate/{provider} alias routes
    /// </summary>
    /// <returns></returns>
    public AuthFeature AddAuthenticateAliasRoutes()
    {
        ServiceRoutes[typeof(AuthenticateService)] =
        [
            "/" + LocalizedStrings.Auth.Localize(),
            "/" + LocalizedStrings.Auth.Localize() + "/{provider}",
            "/" + LocalizedStrings.Authenticate.Localize(),
            "/" + LocalizedStrings.Authenticate.Localize() + "/{provider}"
        ];
        return this;
    }

    /// <summary>
    /// The Session to return for AuthSecret
    /// </summary>
    public IAuthSession AuthSecretSession { get; set; }

    public AuthFeature(Action<AuthFeature> configure) : this(() => new AuthUserSession(), TypeConstants<IAuthProvider>.EmptyArray)
    {
        OnBeforeInit.Add(configure);
    }
        
    public AuthFeature(IAuthProvider authProvider) : this(() => new AuthUserSession(), [authProvider]) {}
    public AuthFeature(IEnumerable<IAuthProvider> authProviders) : this(() => new AuthUserSession(), authProviders.ToArray()) {}
    public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders, string htmlRedirect = null)
    {
        this.SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        this.SessionType = sessionFactory().GetType();
        this.authProviders = authProviders;

        ServiceRoutes = new() {
            [typeof(AuthenticateService)] = [
                "/" + LocalizedStrings.Auth.Localize(),
                "/" + LocalizedStrings.Auth.Localize() + "/{provider}"
            ],
            [typeof(AssignRolesService)] = ["/" + LocalizedStrings.AssignRoles.Localize()],
            [typeof(UnAssignRolesService)] = ["/" + LocalizedStrings.UnassignRoles.Localize()],
        };
        ServiceRoutesVerbs = new()
        {
            ["/" + LocalizedStrings.Auth.Localize()] = "GET,POST",
            ["/" + LocalizedStrings.Auth.Localize() + "/{provider}"] = "GET,POST",
        };

        this.HtmlRedirect = htmlRedirect ?? "~/" + LocalizedStrings.Login.Localize();
        this.CreateDigestAuthHashes = authProviders.Any(x => x is DigestAuthProvider);

        FormLayout[0].AllowableValues = [..authProviders.Where(x => x is not IAuthWithRequest).Select(x => x.Provider),"logout"];
    }

    /// <summary>
    /// Use a plugin or OnBeforeInit delegate to register authProvider dynamically. Your plugin can implement `IPreInitPlugin` interface
    /// to call `appHost.GetPlugin&lt;AuthFeature&gt;().RegisterAuthProvider()` before the AuthFeature is registered.
    /// </summary>
    public void RegisterAuthProvider(IAuthProvider authProvider)
    {
        if (hasRegistered)
            throw new Exception("AuthFeature has already been registered");
            
        this.authProviders = new List<IAuthProvider>(this.AuthProviders) {
            authProvider
        }.ToArray();
    }

    /// <summary>
    /// Use a plugin or OnBeforeInit delegate to register authProvider dynamically. Your plugin can implement `IPreInitPlugin` interface
    /// to call `appHost.GetPlugin&lt;AuthFeature&gt;().RegisterAuthProvider()` before the AuthFeature is registered.
    /// </summary>
    public void RegisterAuthProviders(IEnumerable<IAuthProvider> providers)
    {
        var mergedProviders = new List<IAuthProvider>(this.AuthProviders);
        mergedProviders.AddRange(providers);
        this.authProviders = mergedProviders.ToArray();
    }

    private bool hasRegistered;

    public void Register(IAppHost appHost)
    {
        OnBeforeInit.ForEach(x => x(this));

        hasRegistered = true;
        AuthenticateService.Init(SessionFactory, AuthProviders);

        var unitTest = appHost == null;
        if (unitTest) return;

        if (HostContext.StrictMode)
        {
            var sessionInstance = SessionFactory();
            if (TypeSerializer.HasCircularReferences(sessionInstance))
                throw new StrictModeException($"User Session {sessionInstance.GetType().Name} cannot have circular dependencies", "sessionFactory",
                    StrictModeCodes.CyclicalUserSession);
        }

        AuthSecretSession = appHost.Config.AuthSecretSession;

        var serviceLookup = ServiceRoutes.GroupBy(x => x.Key);
        foreach (var lookup in serviceLookup)
        {
            var serviceType = lookup.Key;
            appHost.ServiceController.RegisterService(serviceType);
            var defaultVerbs = serviceType.GetVerbs();

            var reqAttr = serviceType.FirstAttribute<DefaultRequestAttribute>();
            if (reqAttr != null)
            {
                foreach (var entry in lookup)
                {
                    foreach (var atPath in entry.Value)
                    {
                        var verbs = ServiceRoutesVerbs.TryGetValue(atPath, out var v) ? v : defaultVerbs;
                        appHost.Routes.Add(reqAttr.RequestType, atPath, verbs);
                    }
                }
            }
        }
            
        appHost.ConfigureOperation<Authenticate>(op => op.FormLayout = FormLayout);
        appHost.ConfigureOperation<AssignRoles>(op => op.AddRole(RoleNames.Admin));
        appHost.ConfigureOperation<UnAssignRoles>(op => op.AddRole(RoleNames.Admin));

        if (ProfileImages != null)
        {
            appHost.RawHttpHandlers.Add(req => req.PathInfo.Contains(ProfileImages.Path)
                ? ProfileImages
                : null);
        }

        var sessionFeature = RegisterPlugins.OfType<SessionFeature>().FirstOrDefault();
        if (sessionFeature != null)
        {
            sessionFeature.SessionExpiry = SessionExpiry;
            sessionFeature.PermanentSessionExpiry = PermanentSessionExpiry;
        }

        if (RegisterPlugins.Count > 0)
        {
            appHost.LoadPlugin(RegisterPlugins.ToArray());
        }

        if (IncludeAuthMetadataProvider && appHost.TryResolve<IAuthMetadataProvider>() == null)
            appHost.Register<IAuthMetadataProvider>(new AuthMetadataProvider());

        if (!appHost.CustomErrorHttpHandlers.ContainsKey(HttpStatusCode.Unauthorized))
            appHost.CustomErrorHttpHandlers[HttpStatusCode.Unauthorized] = new AuthFeatureUnauthorizedHttpHandler(this);
        if (!appHost.CustomErrorHttpHandlers.ContainsKey(HttpStatusCode.Forbidden))
            appHost.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = new AuthFeatureAccessDeniedHttpHandler(this);
        if (!appHost.CustomErrorHttpHandlers.ContainsKey(HttpStatusCode.PaymentRequired))
            appHost.CustomErrorHttpHandlers[HttpStatusCode.PaymentRequired] = new AuthFeatureAccessDeniedHttpHandler(this);

        AuthProviders.OfType<IAuthPlugin>().Each(x => x.Register(appHost, this));

        AuthenticateService.HtmlRedirect = HtmlRedirect;
        AuthenticateService.HtmlRedirectAccessDenied = HtmlRedirectAccessDenied;
        AuthenticateService.HtmlRedirectReturnParam = HtmlRedirectReturnParam;
        AuthenticateService.HtmlRedirectReturnPathOnly = HtmlRedirectReturnPathOnly;            
        AuthenticateService.AuthResponseDecorator = AuthResponseDecorator;
        if (ValidateFn != null)
            AuthenticateService.ValidateFn = ValidateFn;

        var authNavItems = AuthProviders.Select(x => (x as AuthProvider)?.NavItem).Where(x => x != null);
        if (!ViewUtils.NavItemsMap.TryGetValue("auth", out var navItems))
            ViewUtils.NavItemsMap["auth"] = navItems = [];

        var isDefaultHtmlRedirect = HtmlRedirect == "~/" + LocalizedStrings.Login.Localize();
        if (IncludeDefaultLogin && isDefaultHtmlRedirect && !appHost.VirtualFileSources.FileExists("/login.html"))
        {
            appHost.VirtualFileSources.GetMemoryVirtualFiles().WriteFile("/login.html", 
                Templates.HtmlTemplates.GetLoginTemplate());
            // required when not using feature like SharpPagesFeature to auto map /login => /login.html
            appHost.CatchAllHandlers.Add(httpReq => httpReq.PathInfo == "/login"
                ? new StaticFileHandler(HostContext.VirtualFileSources.GetFile("/login.html"))
                : null);
        }

        navItems.AddRange(authNavItems);

        var uiFeature = appHost.GetPlugin<UiFeature>();
        appHost.AddToAppMetadata(meta => {
            meta.Plugins.Auth = new AuthInfo {
                HasAuthSecret = (appHost.Config.AdminAuthSecret != null).NullIfFalse(),
                HasAuthRepository = appHost.GetContainer().Exists<IAuthRepository>().NullIfFalse(),
                IncludesRoles = IncludeRolesInAuthenticateResponse.NullIfFalse(),
                IncludesOAuthTokens = IncludeOAuthTokensInAuthenticateResponse.NullIfFalse(),
                HtmlRedirect = HtmlRedirect?.TrimStart('~'),
                ServiceRoutes = ServiceRoutes.ToMetadataServiceRoutes(routes => {
                    var register = appHost.GetPlugin<RegistrationFeature>();
                    if (register != null)
                        routes[nameof(RegisterService)] = [register.AtRestPath];
                }),
                AuthProviders = AuthenticateService.GetAuthProviders()
                    .OrderBy(x => (x as AuthProvider)?.Sort ?? 0)
                    .Map(x => new MetaAuthProvider {
                        Type = x.Type,
                        Name = x.Provider,
                        Label = (x as AuthProvider)?.Label,
                        Icon = (x as AuthProvider)?.Icon,
                        NavItem = (x as AuthProvider)?.NavItem,
                        FormLayout = (x as AuthProvider)?.FormLayout,
                        Meta = x.Meta,
                    }),
                RoleLinks = uiFeature?.RoleLinks ?? new(),
            };
            if (meta.Plugins.Auth.HasAuthSecret == true && AdminAuthSecretInfo != null)
                meta.Plugins.Auth.AuthProviders.Add(AdminAuthSecretInfo);
        });

        OnAfterInit.ForEach(x => x(this));
    }

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        var authEvents = appHost.TryResolve<IAuthEvents>();
        if (authEvents == null)
        {
            authEvents = AuthEvents.Count == 0
                ? new AuthEvents() :
                AuthEvents.Count == 1
                    ? AuthEvents.First()
                    : new MultiAuthEvents(AuthEvents);

            appHost.GetContainer().Register(authEvents);
        }
        else if (AuthEvents.Count > 0)
        {
            throw new Exception("Registering IAuthEvents via both AuthFeature.AuthEvents and IOC is not allowed");
        }
    }

    public IAuthProvider GetAuthProvider(string provider) => AuthenticateService.GetAuthProvider(provider);
    public JwtAuthProviderReader GetJwtAuthProviderReader() => AuthenticateService.GetJwtAuthProvider();
    public JwtAuthProvider GetRequiredJwtAuthProvider() => (JwtAuthProvider)AuthenticateService.GetRequiredJwtAuthProvider();
}

public static class AuthFeatureExtensions
{
    public static string GetHtmlRedirect(this AuthFeature feature)
    {
        if (feature != null)
            return feature.HtmlRedirect;

        return "~/" + HostContext.ResolveLocalizedString(LocalizedStrings.Login);
    }

    public static string GetHtmlRedirectUrl(this AuthFeature feature, IRequest req) =>
        feature.GetHtmlRedirectUrl(req, feature.HtmlRedirectAccessDenied ?? feature.HtmlRedirect, includeRedirectParam: true);
        
    public static string GetHtmlRedirectUrl(this AuthFeature feature, IRequest req, string redirectUrl, bool includeRedirectParam)
    {
        var url = req.ResolveAbsoluteUrl(redirectUrl);
        if (includeRedirectParam)
        {
            var redirectPath = !feature.HtmlRedirectReturnPathOnly
                ? req.ResolveAbsoluteUrl("~" + req.PathInfo + ToQueryString(req.QueryString))
                : req.PathInfo + ToQueryString(req.QueryString);

            var returnParam = HostContext.ResolveLocalizedString(feature.HtmlRedirectReturnParam) ??
                              HostContext.ResolveLocalizedString(LocalizedStrings.Redirect);

            if (url.IndexOf("?" + returnParam, StringComparison.OrdinalIgnoreCase) == -1 &&
                url.IndexOf("&" + returnParam, StringComparison.OrdinalIgnoreCase) == -1)
            {
                return url.AddQueryParam(returnParam, redirectPath);
            }
        }
        return url;
    }
        
    public static void DoHtmlRedirect(this AuthFeature feature, string redirectUrl, IRequest req, IResponse res, bool includeRedirectParam)
    {
        var url = feature.GetHtmlRedirectUrl(req, redirectUrl, includeRedirectParam);
        res.RedirectToUrl(url);
    }
        
    private static string ToQueryString(NameValueCollection queryStringCollection)
    {
        if (queryStringCollection == null || queryStringCollection.Count == 0)
            return string.Empty;

        return "?" + queryStringCollection.ToFormUrlEncoded();
    }

    //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
    public static Regex ValidUserNameRegEx = new(@"^(?=.{3,36}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

    public static bool IsValidUsername(this AuthFeature feature, string userName)
    {
        if (feature == null)
            return ValidUserNameRegEx.IsMatch(userName);

        return feature.IsValidUsernameFn?.Invoke(userName) 
               ?? feature.ValidUserNameRegEx.IsMatch(userName);
    }

    public static async Task<IHttpResult> SuccessAuthResultAsync(this IHttpResult result, IServiceBase service, IAuthSession session)
    {
        var feature = HostContext.GetPlugin<AuthFeature>();
        if (result != null && feature != null)
        {
            var hasAuthResponseFilter = feature.AuthProviders.Any(x => x is IAuthResponseFilter);
            if (hasAuthResponseFilter)
            {
                var ctx = new AuthResultContext {
                    Result = result,
                    Service = service,
                    Session = session,
                    Request = service.Request,
                };
                foreach (var responseFilter in feature.AuthProviders.OfType<IAuthResponseFilter>())
                {
                    await responseFilter.ResultFilterAsync(ctx).ConfigAwait();
                }
            }
        }
        return result;
    }

    public static IHttpResult SuccessAuthResult(this IHttpResult result, IServiceBase service, IAuthSession session)
    {
        var feature = HostContext.GetPlugin<AuthFeature>();
        if (result != null && feature != null)
        {
            var hasAuthResponseFilter = feature.AuthProviders.Any(x => x is IAuthResponseFilter);
            if (hasAuthResponseFilter)
            {
                var ctx = new AuthResultContext {
                    Result = result,
                    Service = service,
                    Session = session,
                    Request = service.Request,
                };
                foreach (var responseFilter in feature.AuthProviders.OfType<IAuthResponseFilter>())
                {
                    responseFilter.ResultFilterAsync(ctx).Wait();
                }
            }
        }
        return result;
    }
        
    public static Task HandleFailedAuth(this IAuthProvider authProvider,
        IAuthSession session, IRequest httpReq, IResponse httpRes)
    {
        if (authProvider is AuthProvider baseAuthProvider)
            return baseAuthProvider.OnFailedAuthentication(session, httpReq, httpRes);

        httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
        httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"{authProvider.Provider} realm=\"{authProvider.AuthRealm}\"");
        return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
    }
        
}
    
public class AuthFeatureUnauthorizedHttpHandler(AuthFeature feature) : HttpAsyncTaskHandler
{
    public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
    {
        if (feature.HtmlRedirect != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
        {
            var url = feature.GetHtmlRedirectUrl(req, feature.HtmlRedirect, includeRedirectParam:true);
            res.RedirectToUrl(url);
            return TypeConstants.EmptyTask;
        }

        if (res.StatusCode < 300)
            res.StatusCode = (int)HttpStatusCode.Unauthorized;
        if (string.IsNullOrEmpty(res.GetHeader(HttpHeaders.WwwAuthenticate)))
        {
            var iAuthProvider = feature.AuthProviders.First(); 
            res.AddHeader(HttpHeaders.WwwAuthenticate, $"{iAuthProvider.Provider} realm=\"{iAuthProvider.AuthRealm}\"");
        }
            
        var doJsonp = HostContext.Config.AllowJsonpRequests && !string.IsNullOrEmpty(req.GetJsonpCallback());
        if (doJsonp)
        {
            var errorMessage = res.StatusDescription ?? ErrorMessages.NotAuthenticated;
            return res.WriteErrorToResponse(req, MimeTypes.Json, null, 
                errorMessage, HttpError.Unauthorized(errorMessage), res.StatusCode);
        }

        return res.EndHttpHandlerRequestAsync();
    }

    public override bool IsReusable => true;
    public override bool RunAsAsync() => true;
}
    
public class AuthFeatureAccessDeniedHttpHandler(AuthFeature feature) : ForbiddenHttpHandler
{
    public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
    {
        if (feature.HtmlRedirectAccessDenied != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
        {
            var url = feature.GetHtmlRedirectUrl(req, feature.HtmlRedirectAccessDenied, includeRedirectParam:false);
            res.RedirectToUrl(url);
            return TypeConstants.EmptyTask;
        }

        var doJsonp = HostContext.Config.AllowJsonpRequests && !string.IsNullOrEmpty(req.GetJsonpCallback());
        if (doJsonp)
        {
            var errorMessage = res.StatusDescription ?? ErrorMessages.AccessDenied;
            return res.WriteErrorToResponse(req, MimeTypes.Json, null, 
                errorMessage, HttpError.Forbidden(errorMessage), res.StatusCode);
        }

        res.ContentType = "text/plain";
        return res.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r => {
            var sb = CreateForbiddenResponseTextBody(req);

            return res.OutputStream.WriteAsync(StringBuilderCache.ReturnAndFree(sb));
        });
    }
}