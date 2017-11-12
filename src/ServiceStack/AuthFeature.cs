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
        private readonly IAuthProvider[] authProviders;

        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public List<IPlugin> RegisterPlugins { get; set; }

        public List<IAuthEvents> AuthEvents { get; set; }

        public string HtmlRedirect { get; set; }

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

            string localize(string s) => HostContext.AppHost.ResolveLocalizedString(s, null);

            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AuthenticateService), new[]
                    {
                        "/" + localize(LocalizedStrings.Auth),
                        "/" + localize(LocalizedStrings.Auth) + "/{provider}",
                        "/" + localize(LocalizedStrings.Authenticate),
                        "/" + localize(LocalizedStrings.Authenticate) + "/{provider}",
                    } },
                { typeof(AssignRolesService), new[]{ "/" + localize(LocalizedStrings.AssignRoles) } },
                { typeof(UnAssignRolesService), new[]{ "/" + localize(LocalizedStrings.UnassignRoles) } },
            };

            RegisterPlugins = new List<IPlugin> {
                new SessionFeature()
            };

            AuthEvents = new List<IAuthEvents>();

            this.HtmlRedirect = htmlRedirect ?? "~/" + localize(LocalizedStrings.Login);
            this.IncludeAuthMetadataProvider = true;
            this.ValidateUniqueEmails = true;
            this.DeleteSessionCookiesOnLogout = true;
            this.GenerateNewSessionCookiesOnAuthentication = true;
            this.CreateDigestAuthHashes = authProviders.Any(x => x is DigestAuthProvider);
        }

        public void Register(IAppHost appHost)
        {
            AuthenticateService.Init(sessionFactory, authProviders);

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

            authProviders.OfType<IAuthPlugin>().Each(x => x.Register(appHost, this));

            AuthenticateService.HtmlRedirect = HtmlRedirect;
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