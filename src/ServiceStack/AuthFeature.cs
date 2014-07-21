using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Auth;

namespace ServiceStack
{
    /// <summary>
    /// Enable the authentication feature and configure the AuthService.
    /// </summary>
    public class AuthFeature : IPlugin
    {
        public static bool AddUserIdHttpHeader = true;

        private readonly Func<IAuthSession> sessionFactory;
        private readonly IAuthProvider[] authProviders;

        public Dictionary<Type, string[]> ServiceRoutes { get; set; }
        public List<IPlugin> RegisterPlugins { get; set; }

        public string HtmlRedirect { get; set; }

        public bool IncludeAuthMetadataProvider { get; set; }

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

            Func<string,string> localize = HostContext.ResolveLocalizedString;

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

            this.HtmlRedirect = htmlRedirect ?? "~/" + localize(LocalizedStrings.Login);
            this.IncludeAuthMetadataProvider = true;
        }

        public void Register(IAppHost appHost)
        {
            AuthenticateService.Init(sessionFactory, authProviders);
            AuthenticateService.HtmlRedirect = HtmlRedirect;

            var unitTest = appHost == null;
            if (unitTest) return;

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            RegisterPlugins.ForEach(x => appHost.LoadPlugin(x));

            if (IncludeAuthMetadataProvider && appHost.TryResolve<IAuthMetadataProvider>() == null)
                appHost.Register<IAuthMetadataProvider>(new AuthMetadataProvider());
        }

        public TimeSpan GetDefaultSessionExpiry()
        {
            var authProvider = authProviders.FirstOrDefault() as AuthProvider;
            return authProvider != null 
                ? authProvider.SessionExpiry
                : SessionFeature.DefaultSessionExpiry;
        }
    }
}