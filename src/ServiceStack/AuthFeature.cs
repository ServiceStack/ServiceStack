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

        public string HtmlRedirectParam { get; set; }

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

        public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders, string htmlRedirect = "~/login", string htmlRedirectParam = "redirect")
        {
            this.sessionFactory = sessionFactory;
            this.authProviders = authProviders;

            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AuthenticateService), new[]{ "/auth", "/auth/{provider}", "/authenticate", "/authenticate/{provider}", } },
                { typeof(AssignRolesService), new[]{ "/assignroles" } },
                { typeof(UnAssignRolesService), new[]{ "/unassignroles" } },
            };

            RegisterPlugins = new List<IPlugin> {
                new SessionFeature()                          
            };

            this.HtmlRedirect = htmlRedirect;

            this.HtmlRedirectParam = htmlRedirectParam;
        }

        public void Register(IAppHost appHost)
        {
            AuthenticateService.Init(sessionFactory, authProviders);
            AuthenticateService.HtmlRedirect = HtmlRedirect;
            AuthenticateService.HtmlRedirectParam = HtmlRedirectParam;

            var unitTest = appHost == null;
            if (unitTest) return;

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            RegisterPlugins.ForEach(x => appHost.LoadPlugin(x));
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
