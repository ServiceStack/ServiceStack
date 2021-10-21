﻿using System;
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
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
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

            if (redirect.StartsWith("//") || redirect.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                if (redirect.StartsWith(req.GetBaseUrl()))
                    return;
                
                throw new ArgumentException(ErrorMessages.NoExternalRedirects, Keywords.Continue);
            }
        }

        public Func<IAuthSession> SessionFactory { get; set; }
        private IAuthProvider[] authProviders;
        public IAuthProvider[] AuthProviders => authProviders;

        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public List<IPlugin> RegisterPlugins { get; set; } = new() {
            new SessionFeature()
        };

        public List<IAuthEvents> AuthEvents { get; set; } = new();

        /// <summary>
        /// Invoked before AuthFeature is registered
        /// </summary>
        public Action<AuthFeature> OnBeforeInit { get; set; }

        /// <summary>
        /// Invoked after AuthFeature is registered
        /// </summary>
        public Action<AuthFeature> OnAfterInit { get; set; }
        
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
            ServiceRoutes[typeof(AuthenticateService)] = new[] {
                "/" + LocalizedStrings.Auth.Localize(),
                "/" + LocalizedStrings.Auth.Localize() + "/{provider}",
            };
            return this;
        }

        /// <summary>
        /// Add /authenticate and /authenticate/{provider} alias routes
        /// </summary>
        /// <returns></returns>
        public AuthFeature AddAuthenticateAliasRoutes()
        {
            ServiceRoutes[typeof(AuthenticateService)] = new[] {
                "/" + LocalizedStrings.Auth.Localize(),
                "/" + LocalizedStrings.Auth.Localize() + "/{provider}",
                "/" + LocalizedStrings.Authenticate.Localize(),
                "/" + LocalizedStrings.Authenticate.Localize() + "/{provider}",
            };
            return this;
        }

        /// <summary>
        /// The Session to return for AuthSecret
        /// </summary>
        public IAuthSession AuthSecretSession { get; set; }

        public AuthFeature(Action<AuthFeature> configure) : this(() => new AuthUserSession(), TypeConstants<IAuthProvider>.EmptyArray)
        {
            OnBeforeInit = configure;
        }
        
        public AuthFeature(IAuthProvider authProvider) : this(() => new AuthUserSession(), new []{ authProvider }) {}
        public AuthFeature(IEnumerable<IAuthProvider> authProviders) : this(() => new AuthUserSession(), authProviders.ToArray()) {}
        public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders, string htmlRedirect = null)
        {
            this.SessionFactory = sessionFactory;
            this.authProviders = authProviders;

            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AuthenticateService), new[]
                    {
                        "/" + LocalizedStrings.Auth.Localize(),
                        "/" + LocalizedStrings.Auth.Localize() + "/{provider}",
                    } },
                { typeof(AssignRolesService), new[]{ "/" + LocalizedStrings.AssignRoles.Localize() } },
                { typeof(UnAssignRolesService), new[]{ "/" + LocalizedStrings.UnassignRoles.Localize() } },
            };

            this.HtmlRedirect = htmlRedirect ?? "~/" + LocalizedStrings.Login.Localize();
            this.CreateDigestAuthHashes = authProviders.Any(x => x is DigestAuthProvider);
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
            OnBeforeInit?.Invoke(this);

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

            appHost.RegisterServices(ServiceRoutes);

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

            appHost.CustomErrorHttpHandlers[HttpStatusCode.Unauthorized] = new AuthFeatureUnauthorizedHttpHandler(this);
            appHost.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = new AuthFeatureAccessDeniedHttpHandler(this);
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
                ViewUtils.NavItemsMap["auth"] = navItems = new List<NavItem>();

            var isDefaultHtmlRedirect = HtmlRedirect == "~/" + LocalizedStrings.Login.Localize();
            if (IncludeDefaultLogin && isDefaultHtmlRedirect && !appHost.VirtualFileSources.FileExists("/login.html"))
            {
                appHost.VirtualFileSources.GetMemoryVirtualFiles().WriteFile("/login.html", 
                    Templates.HtmlTemplates.GetLoginTemplate());
                // required when not using feature like SharpPagesFeature to auto map /login => /login.html
                appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => pathInfo == "/login"
                    ? new Host.Handlers.StaticFileHandler(HostContext.VirtualFileSources.GetFile("/login.html"))
                    : null);
            }

            navItems.AddRange(authNavItems);

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
                            routes[nameof(RegisterService)] = new []{ register.AtRestPath };
                    }),
                    AuthProviders = AuthenticateService.GetAuthProviders().Map(x => new MetaAuthProvider {
                        Type = x.Type,
                        Name = x.Provider,
                        NavItem = (x as AuthProvider)?.NavItem,
                        Meta = x.Meta,
                    })
                };
            });

            OnAfterInit?.Invoke(this);
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
        public static Regex ValidUserNameRegEx = new(@"^(?=.{3,20}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

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
    
    public class AuthFeatureUnauthorizedHttpHandler : HttpAsyncTaskHandler
    {
        private readonly AuthFeature feature;
        public AuthFeatureUnauthorizedHttpHandler(AuthFeature feature) => this.feature = feature;
        
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
            return res.EndHttpHandlerRequestAsync();
        }

        public override bool IsReusable => true;
        public override bool RunAsAsync() => true;
    }
    
    public class AuthFeatureAccessDeniedHttpHandler : ForbiddenHttpHandler
    {
        private readonly AuthFeature feature;
        public AuthFeatureAccessDeniedHttpHandler(AuthFeature feature) => this.feature = feature;

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (feature.HtmlRedirectAccessDenied != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                var url = feature.GetHtmlRedirectUrl(req, feature.HtmlRedirectAccessDenied, includeRedirectParam:false);
                res.RedirectToUrl(url);
                return TypeConstants.EmptyTask;
            }

            res.ContentType = "text/plain";
            return res.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r => {
                var sb = CreateForbiddenResponseTextBody(req);

                return res.OutputStream.WriteAsync(StringBuilderCache.ReturnAndFree(sb));
            });
        }
    }
    
}