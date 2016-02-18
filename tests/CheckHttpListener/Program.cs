using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using Funq;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace CheckHttpListener
{
    [DataContract]
    public class CustomUserSession : AuthUserSession
    {
        private int organisationId_;

        [DataMember]
        public int UtcOffset { get; set; }

        [DataMember]
        public Int32 CreatedDateUnix { get; set; }

        [DataMember]
        public DateTime CreatedDate { get; set; }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            CreatedDate = DateTime.Now;

            CreatedDateUnix = (Int32)(CreatedDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            authService.SaveSession(this);

        }
    }

    public class CustomBasicAuthProvider : BasicAuthProvider
    {
        public CustomBasicAuthProvider() {}

        public CustomBasicAuthProvider(IAppSettings appSettings)
            : base(appSettings) {}

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            return base.Authenticate(authService, session, request);
        }

        /// <summary>
        ///     This method will only send the WWW-Authenticate response header when the User-Agent request
        ///     header contains servicestack i.e. it's the servicestack client and not a browser
        /// </summary>
        public override void OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            //Only return digest header if User-Agent is the ServiceStack .NET Client
            //expected user agent value is: "ServiceStack .NET Client {Version}"
            if (httpReq.Headers["User-Agent"].ToLower().Contains("servicestack"))
            {
                httpRes.AddHeader("WWW-Authenticate",
                    "{0} realm=\"{1}\"".Fmt(Provider, AuthRealm));
            }

            httpRes.StatusCode = 401;
            httpRes.EndRequest(false);
        }
    }

    public static class AppHostConfiguration
    {
        public static void Configure(ServiceStackHost appHost, Container container)
        {
            var appSettings = new AppSettings();

            var auth = new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[]
                {
                    new CustomBasicAuthProvider(),
                    new CredentialsAuthProvider(appSettings) {SessionExpiry = TimeSpan.FromMinutes(30)}
                },
                "/login")
            {
                GenerateNewSessionCookiesOnAuthentication = false,
            };

            appHost.Plugins.Add(auth);

            IUserAuthRepository authRepository = new InMemoryAuthRepository();
            ICacheClient cacheClient = new MemoryCacheClient();

            var testUser = authRepository.CreateUserAuth(new UserAuth { Email = "a@b.com" }, "a");


            //IoC registrations
            container.Register(cacheClient);
            container.Register(authRepository);


            var hostConfig = new HostConfig
            {
                DebugMode = true,
                AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Csv },                
            };


            appHost.SetConfig(hostConfig);
        }

        public static void Start() {}

        public static void Stop(bool immediate) {}
    }

    [Route("/test2")]
    public class TestNoAuthRequest : IReturn<string>
    {
    }

    [Route("/test")]
    public class TestRequest : IReturn<string>
    {
    }

    public class TestService : Service
    {
        private CustomUserSession session_;

        protected CustomUserSession Session
        {
            get
            {
                return session_ ?? (session_ = SessionAs<CustomUserSession>());
            }
        }

        [Authenticate]
        public string Any(TestRequest request)
        {
            return Session.UserAuthId;
        }

        public string Any(TestNoAuthRequest request)
        {
            return "Hi";
        }
    }

    public class AppSelfHost : AppSelfHostBase
    {
        public AppSelfHost()
            : base("DocuRec Services", typeof(TestService).Assembly)
        { }

        public override void Configure(Container container)
        {
            AppHostConfiguration.Configure(this, container);
        }

        /// <summary>
        ///     Starts the ServiceStackHost and Schedules jobs (if it's the first time being called)
        /// </summary>
        public override ServiceStackHost Start(string urlBase)
        {
            return Start(new List<string> { urlBase });
        }

        /// <summary>
        ///     Starts the ServiceStackHost and Schedules jobs (if it's the first time being called)
        /// </summary>
        public override ServiceStackHost Start(IEnumerable<string> urlBases)
        {
            AppHostConfiguration.Start();

            return base.Start(urlBases);
        }

        public override void Stop()
        {
            AppHostConfiguration.Stop(true);

            base.Stop();
        }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
            var appHost = new AppSelfHost();
            appHost.Init();
            appHost.Start("http://127.0.0.1:1234/");

            Thread.Sleep(2500);

            var client = new JsonServiceClient("http://127.0.0.1:1234/")
            {
                AlwaysSendBasicAuthHeader = true,
                UserName = "a@b.com",
                Password = "a"
            };

            var post1 = client.Post(new TestRequest());
            var post2 = client.Post(new TestRequest());
            var response = "First response: {0}, Second Response: {1}".Fmt(post1, post2);

            Process.Start("http://127.0.0.1:1234/");

            Console.Out.WriteLine(response);
            Console.Read();
        }
    }
}