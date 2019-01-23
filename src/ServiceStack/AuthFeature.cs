using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack
{
    /// <summary>
    /// Enable the authentication feature and configure the AuthService.
    /// </summary>
    public class AuthFeature : IPlugin, IPostInitPlugin
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = AuthFeatureExtensions.ValidUserNameRegEx;

        public Func<string, bool> IsValidUsernameFn { get; set; }

        private readonly Func<IAuthSession> sessionFactory;
        private IAuthProvider[] authProviders;
        public IAuthProvider[] AuthProviders => authProviders;

        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public List<IPlugin> RegisterPlugins { get; set; }

        public List<IAuthEvents> AuthEvents { get; set; }

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
        public string HtmlRedirectReturnParam { get; set; }

        /// <summary>
        /// Whether to only capture return path or absolute URL (default)
        /// </summary>
        public bool HtmlRedirectReturnPathOnly { get; set; }

        public string HtmlLogoutRedirect { get; set; }

        public bool IncludeAuthMetadataProvider { get; set; }

        public bool ValidateUniqueEmails { get; set; }

        public bool ValidateUniqueUserNames { get; set; }

        public bool DeleteSessionCookiesOnLogout { get; set; }

        public bool GenerateNewSessionCookiesOnAuthentication { get; set; }
        
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

        public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders, string htmlRedirect = null)
        {
            this.sessionFactory = sessionFactory;
            this.authProviders = authProviders;

            string Localize(string s) => HostContext.AppHost?.ResolveLocalizedString(s, null) ?? s;

            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AuthenticateService), new[]
                    {
                        "/" + Localize(LocalizedStrings.Auth),
                        "/" + Localize(LocalizedStrings.Auth) + "/{provider}",
                        "/" + Localize(LocalizedStrings.Authenticate),
                        "/" + Localize(LocalizedStrings.Authenticate) + "/{provider}",
                    } },
                { typeof(AssignRolesService), new[]{ "/" + Localize(LocalizedStrings.AssignRoles) } },
                { typeof(UnAssignRolesService), new[]{ "/" + Localize(LocalizedStrings.UnassignRoles) } },
            };

            RegisterPlugins = new List<IPlugin> {
                new SessionFeature()
            };

            AuthEvents = new List<IAuthEvents>();

            this.HtmlRedirect = htmlRedirect ?? "~/" + Localize(LocalizedStrings.Login);
            this.HtmlRedirectReturnParam = LocalizedStrings.Redirect;
            this.IncludeAuthMetadataProvider = true;
            this.ValidateUniqueEmails = true;
            this.DeleteSessionCookiesOnLogout = true;
            this.GenerateNewSessionCookiesOnAuthentication = true;
            this.CreateDigestAuthHashes = authProviders.Any(x => x is DigestAuthProvider);
        }

        /// <summary>
        /// Use a plugin to register authProvider dynamically. Your plugin can implement `IPreInitPlugin` interface
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

        private bool hasRegistered;

        public void Register(IAppHost appHost)
        {
            hasRegistered = true;
            AuthenticateService.Init(sessionFactory, AuthProviders);

            var unitTest = appHost == null;
            if (unitTest) return;

            if (HostContext.StrictMode)
            {
                var sessionInstance = sessionFactory();
                if (TypeSerializer.HasCircularReferences(sessionInstance))
                    throw new StrictModeException($"User Session {sessionInstance.GetType().Name} cannot have circular dependencies", "sessionFactory",
                        StrictModeCodes.CyclicalUserSession);
            }

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            var sessionFeature = RegisterPlugins.OfType<SessionFeature>().First();
            sessionFeature.SessionExpiry = SessionExpiry;
            sessionFeature.PermanentSessionExpiry = PermanentSessionExpiry;

            appHost.LoadPlugin(RegisterPlugins.ToArray());

            if (IncludeAuthMetadataProvider && appHost.TryResolve<IAuthMetadataProvider>() == null)
                appHost.Register<IAuthMetadataProvider>(new AuthMetadataProvider());

            AuthProviders.OfType<IAuthPlugin>().Each(x => x.Register(appHost, this));

            AuthenticateService.HtmlRedirect = HtmlRedirect;
            AuthenticateService.HtmlRedirectAccessDenied = HtmlRedirectAccessDenied;
            AuthenticateService.HtmlRedirectReturnParam = HtmlRedirectReturnParam;
            AuthenticateService.HtmlRedirectReturnPathOnly = HtmlRedirectReturnPathOnly;            
            AuthenticateService.AuthResponseDecorator = AuthResponseDecorator;
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

        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public static Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,20}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        public static bool IsValidUsername(this AuthFeature feature, string userName)
        {
            if (feature == null)
                return ValidUserNameRegEx.IsMatch(userName);

            return feature.IsValidUsernameFn?.Invoke(userName) 
                ?? feature.ValidUserNameRegEx.IsMatch(userName);
        }
    }
}