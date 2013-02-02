using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
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

        public AuthFeature(Func<IAuthSession> sessionFactory, IAuthProvider[] authProviders)
        {
            this.sessionFactory = sessionFactory;
            this.authProviders = authProviders;

            ServiceRoutes = new Dictionary<Type, string[]> {
                { typeof(AuthService), new[]{"/auth", "/auth/{provider}"} },
                { typeof(AssignRolesService), new[]{"/assignroles"} },
                { typeof(UnAssignRolesService), new[]{"/unassignroles"} },
            };
            RegisterPlugins = new List<IPlugin> {
                new SessionFeature()                          
            };
            this.HtmlRedirect = "~/login";
        }

        public void Register(IAppHost appHost)
        {
            AuthService.Init(sessionFactory, authProviders);
            AuthService.HtmlRedirect = HtmlRedirect;

            var unitTest = appHost == null;
            if (unitTest) return;

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            RegisterPlugins.ForEach(x => appHost.LoadPlugin(x));
        }

        public static TimeSpan? GetDefaultSessionExpiry()
        {
            var authProvider = AuthService.AuthProviders.FirstOrDefault() as AuthProvider;
            return authProvider == null ? null : authProvider.SessionExpiry;
        }
    }
}