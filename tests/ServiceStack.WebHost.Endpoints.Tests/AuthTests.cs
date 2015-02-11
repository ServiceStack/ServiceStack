using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Common.Tests.ServiceClient.Web;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
using ServiceStack.WebHost.IntegrationTests.Services;
using System.Collections.Generic;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/secured")]
    public class Secured : IReturn<SecuredResponse>
    {
        public string Name { get; set; }
    }

    public class SecuredResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/securedfileupload")]
    public class SecuredFileUpload
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
    }

    [Authenticate]
    public class SecuredService : Service
    {
        public object Post(Secured request)
        {
            return new SecuredResponse { Result = request.Name };
        }

        public object Get(Secured request)
        {
            throw new ArgumentException("unicorn nuggets");
        }

        public object Post(SecuredFileUpload request)
        {
            var file = this.Request.Files[0];
            return new FileUploadResponse
            {
                FileName = file.FileName,
                ContentLength = file.ContentLength,
                ContentType = file.ContentType,
                Contents = new StreamReader(file.InputStream).ReadToEnd(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName
            };
        }
    }

    public class RequiresRole
    {
        public string Name { get; set; }
    }

    public class RequiresRoleResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [RequiredRole("TheRole")]
    public class RequiresRoleService : Service
    {
        public object Any(RequiresRole request)
        {
            return new RequiresRoleResponse { Result = request.Name };
        }
    }

    public class RequiresAnyRole
    {
        public List<string> Roles { get; set; }

        public RequiresAnyRole()
        {
            Roles = new List<string>();
        }
    }

    public class RequiresAnyRoleResponse
    {
        public List<string> Result { get; set; }

        public ResponseStatus RepsonseStatus { get; set; }

        public RequiresAnyRoleResponse()
        {
            Result = new List<string>();
        }
    }

    [RequiresAnyRole("TheRole", "TheRole2")]
    public class RequiresAnyRoleService : Service
    {
        public object Any(RequiresAnyRole request)
        {
            return new RequiresAnyRoleResponse { Result = request.Roles };
        }
    }

    public class RequiresPermission
    {
        public string Name { get; set; }
    }

    public class RequiresPermissionResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [RequiredPermission("ThePermission")]
    public class RequiresPermissionService : Service
    {
        public RequiresPermissionResponse Any(RequiresPermission request)
        {
            return new RequiresPermissionResponse { Result = request.Name };
        }
    }

    public class RequiresAnyPermission
    {
        public List<string> Permissions { get; set; }

        public RequiresAnyPermission()
        {
            Permissions = new List<string>();
        }
    }

    public class RequiresAnyPermissionResponse
    {
        public List<string> Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }

        public RequiresAnyPermissionResponse()
        {
            Result = new List<string>();
        }
    }

    [RequiresAnyPermission("ThePermission", "ThePermission2")]
    public class RequiresAnyPermissionService : Service
    {
        public RequiresAnyPermissionResponse Any(RequiresAnyPermission request)
        {
            return new RequiresAnyPermissionResponse { Result = request.Permissions };
        }
    }

    public class CustomUserSession : WebSudoAuthUserSession
    {
        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            if (session.UserName == AuthTests.UserNameWithSessionRedirect)
                session.ReferrerUrl = AuthTests.SessionRedirectUrl;
        }
    }

    public class CustomAuthProvider : AuthProvider
    {
        public CustomAuthProvider()
        {
            this.Provider = "custom";
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return false;
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            throw new NotImplementedException();
        }
    }

    public class RequiresCustomAuth
    {
        public string Name { get; set; }
    }

    public class RequiresCustomAuthResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Authenticate(Provider = "custom")]
    public class RequiresCustomAuthService : Service
    {
        public RequiresCustomAuthResponse Any(RequiresCustomAuth request)
        {
            return new RequiresCustomAuthResponse { Result = request.Name };
        }
    }

    public class CustomAuthenticateAttribute : AuthenticateAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            req.Items["TriedMyOwnAuthFirst"] = true; // let's simulate some sort of auth _before_ relaying to base class.

            base.Execute(req, res, requestDto);
        }
    }

    public class CustomAuthAttr
    {
        public string Name { get; set; }
    }

    public class CustomAuthAttrResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [CustomAuthenticate]
    public class CustomAuthAttrService : Service
    {
        public CustomAuthAttrResponse Any(CustomAuthAttr request)
        {
            if (!Request.Items.ContainsKey("TriedMyOwnAuthFirst"))
                throw new InvalidOperationException("TriedMyOwnAuthFirst not present.");

            return new CustomAuthAttrResponse { Result = request.Name };
        }
    }

    public class RequiresWebSudo : IReturn<RequiresWebSudoResponse>
    {
        public string Name { get; set; }
    }

    public class RequiresWebSudoResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [WebSudoRequired]
    public class RequiresWebSudoService : Service
    {
        public object Any(RequiresWebSudo request)
        {
            return new RequiresWebSudoResponse { Result = request.Name };
        }
    }

    public class AuthTests
    {
        protected virtual string VirtualDirectory { get { return ""; } }
        protected virtual string ListeningOn { get { return "http://localhost:1337/"; } }
        protected virtual string WebHostUrl { get { return "http://mydomain.com"; } }


        private const string UserName = "user";
        private const string Password = "p@55word";
        public const string UserNameWithSessionRedirect = "user2";
        public const string PasswordForSessionRedirect = "p@55word2";
        public const string SessionRedirectUrl = "specialLandingPage.html";
        public const string LoginUrl = "specialLoginPage.html";
        private const string EmailBasedUsername = "user@email.com";
        private const string PasswordForEmailBasedAccount = "p@55word3";

        public class AuthAppHostHttpListener
            : AppHostHttpListenerBase
        {
            private readonly string webHostUrl;
            private Action<Container> configureFn;

            public AuthAppHostHttpListener(string webHostUrl, Action<Container> configureFn=null)
                : base("Validation Tests", typeof(CustomerService).Assembly)
            {
                this.webHostUrl = webHostUrl;
                this.configureFn = configureFn;
            }

            private InMemoryAuthRepository userRep;

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig { WebHostUrl = webHostUrl, DebugMode = true });

                Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                    new IAuthProvider[] { //Www-Authenticate should contain basic auth, therefore register this provider first
                        new BasicAuthProvider(), //Sign-in with Basic Auth
						new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
                        new CustomAuthProvider()
					}, "~/" + LoginUrl) { RegisterPlugins = { new WebSudoFeature() } });

                container.Register(new MemoryCacheClient());
                userRep = new InMemoryAuthRepository();
                container.Register<IAuthRepository>(userRep);

                if (configureFn != null)
                {
                    configureFn(container);
                }

                CreateUser(1, UserName, null, Password, new List<string> { "TheRole" }, new List<string> { "ThePermission" });
                CreateUser(2, UserNameWithSessionRedirect, null, PasswordForSessionRedirect);
                CreateUser(3, null, EmailBasedUsername, PasswordForEmailBasedAccount);
            }

            private void CreateUser(int id, string username, string email, string password, List<string> roles = null, List<string> permissions = null)
            {
                string hash;
                string salt;
                new SaltedHash().GetHashAndSaltString(password, out hash, out salt);

                userRep.CreateUserAuth(new UserAuth
                {
                    Id = id,
                    DisplayName = "DisplayName",
                    Email = email ?? "as@if{0}.com".Fmt(id),
                    UserName = username,
                    FirstName = "FirstName",
                    LastName = "LastName",
                    PasswordHash = hash,
                    Salt = salt,
                    Roles = roles,
                    Permissions = permissions
                }, password);
            }

            protected override void Dispose(bool disposing)
            {
                // Needed so that when the derived class tests run the same users can be added again.
                userRep.Clear();
                base.Dispose(disposing);
            }
        }

        AuthAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AuthAppHostHttpListener(WebHostUrl, Configure);
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public virtual void Configure(Container container)
        {
        }

        private static void FailOnAsyncError<T>(T response, Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        IServiceClient GetClient()
        {
            return new JsonServiceClient(ListeningOn);
        }

        IServiceClient GetHtmlClient()
        {
            return new HtmlServiceClient(ListeningOn) { BaseUri = ListeningOn };
        }

        IServiceClient GetClientWithUserPassword()
        {
            return new JsonServiceClient(ListeningOn)
            {
                UserName = UserName,
                Password = Password
            };
        }

        [Test]
        public void No_Credentials_throws_UnAuthorized()
        {
            try
            {
                var client = GetClient();
                var request = new Secured { Name = "test" };
                var response = client.Send<SecureResponse>(request);

                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void Authenticate_attribute_respects_provider()
        {
            try
            {
                var client = GetClient();
                var authResponse = client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

                var request = new RequiresCustomAuth { Name = "test" };
                var response = client.Send<RequiresCustomAuthResponse>(request);

                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void PostFile_with_no_Credentials_throws_UnAuthorized()
        {
            try
            {
                var client = GetClient();
                var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());
                client.PostFile<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, MimeTypes.GetMimeType(uploadFile.Name));

                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void PostFile_does_work_with_BasicAuth()
        {
            var client = GetClientWithUserPassword();
            var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());

            var expectedContents = new StreamReader(uploadFile.OpenRead()).ReadToEnd();
            var response = client.PostFile<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, MimeTypes.GetMimeType(uploadFile.Name));
            Assert.That(response.FileName, Is.EqualTo(uploadFile.Name));
            Assert.That(response.ContentLength, Is.EqualTo(uploadFile.Length));
            Assert.That(response.Contents, Is.EqualTo(expectedContents));
        }

        [Test]
        public void PostFileWithRequest_does_work_with_BasicAuth()
        {
            var client = GetClientWithUserPassword();
            var request = new SecuredFileUpload { CustomerId = 123, CustomerName = "Foo" };
            var uploadFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());

            var expectedContents = new StreamReader(uploadFile.OpenRead()).ReadToEnd();
            var response = client.PostFileWithRequest<FileUploadResponse>(ListeningOn + "/securedfileupload", uploadFile, request);
            Assert.That(response.FileName, Is.EqualTo(uploadFile.Name));
            Assert.That(response.ContentLength, Is.EqualTo(uploadFile.Length));
            Assert.That(response.Contents, Is.EqualTo(expectedContents));
            Assert.That(response.CustomerName, Is.EqualTo("Foo"));
            Assert.That(response.CustomerId, Is.EqualTo(123));
        }

        [Test]
        public void Does_work_with_BasicAuth()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var request = new Secured { Name = "test" };
                var response = client.Send<SecureResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Does_always_send_BasicAuth()
        {
            try
            {
                var client = (ServiceClientBase)GetClientWithUserPassword();
                client.AlwaysSendBasicAuthHeader = true;
                client.RequestFilter = req =>
                {
                    bool hasAuthentication = false;
                    foreach (var key in req.Headers.Keys)
                    {
                        if (key.ToString() == "Authorization")
                            hasAuthentication = true;
                    }
                    Assert.IsTrue(hasAuthentication);
                };

                var request = new Secured { Name = "test" };
                var response = client.Send<SecureResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Does_work_with_CredentailsAuth()
        {
            try
            {
                var client = GetClient();

                var authResponse = client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

                authResponse.PrintDump();

                var request = new Secured { Name = "test" };
                var response = client.Send<SecureResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Can_sent_Session_using_Headers()
        {
            try
            {
                var client = GetClient();

                var authResponse = client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

                authResponse.PrintDump();

                var clientWithHeaders = (JsonServiceClient)GetClient();
                clientWithHeaders.Headers["X-ss-pid"] = authResponse.SessionId;
                clientWithHeaders.Headers["X-ss-opt"] = "perm";

                var request = new Secured { Name = "test" };
                var response = clientWithHeaders.Send<SecureResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public async Task Does_work_with_CredentailsAuth_Async()
        {
            var client = GetClient();

            var request = new Secured { Name = "test" };
            var authResponse = await client.SendAsync<AuthenticateResponse>(
                new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

            authResponse.PrintDump();

            var response = await client.SendAsync<SecureResponse>(request);

            Assert.That(response.Result, Is.EqualTo(request.Name));
        }

        [Test]
        public void Can_call_RequiredRole_service_with_BasicAuth()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var request = new RequiresRole { Name = "test" };
                var response = client.Send<RequiresRoleResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Can_call_RequiredRole_service_with_CredentialsAuth_with_Role()
        {
            try
            {
                var client = GetClient();
                var authResponse = client.Send(
                    new Authenticate
                    {
                        provider = CredentialsAuthProvider.Name,
                        UserName = UserName, // Has Role
                        Password = Password,
                        RememberMe = true,
                    });

                var request = new RequiresRole { Name = "test" };
                var response = client.Send<RequiresRoleResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Does_not_allow_RequiredRole_service_with_CredentialsAuth_without_Role()
        {
            try
            {
                var client = GetClient();
                var authResponse = client.Send(
                    new Authenticate
                    {
                        provider = CredentialsAuthProvider.Name,
                        UserName = "user2", // Does not have Role
                        Password = "p@55word2",
                        RememberMe = true,
                    });

                var request = new RequiresRole { Name = "test" };
                var response = client.Send<RequiresRoleResponse>(request);
                Assert.Fail("Should Throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public void RequiredRole_service_returns_unauthorized_if_no_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                var request = new RequiresRole { Name = "test" };
                var response = client.Send<RequiresRoleResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void RequiredRole_service_returns_forbidden_if_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                ((ServiceClientBase)client).UserName = EmailBasedUsername;
                ((ServiceClientBase)client).Password = PasswordForEmailBasedAccount;

                var request = new RequiresRole { Name = "test" };
                var response = client.Send<RequiresRoleResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void Can_call_RequiredPermission_service_with_BasicAuth()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var request = new RequiresPermission { Name = "test" };
                var response = client.Send<RequiresPermissionResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void RequiredPermission_service_returns_unauthorized_if_no_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                var request = new RequiresPermission { Name = "test" };
                var response = client.Send<RequiresPermissionResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void RequiredPermission_service_returns_forbidden_if_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                ((ServiceClientBase)client).UserName = EmailBasedUsername;
                ((ServiceClientBase)client).Password = PasswordForEmailBasedAccount;

                var request = new RequiresPermission { Name = "test" };
                var response = client.Send<RequiresPermissionResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void Does_work_with_CredentailsAuth_Multiple_Times()
        {
            try
            {
                var client = GetClient();

                var authResponse = client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

                Console.WriteLine(authResponse.Dump());

                for (int i = 0; i < 500; i++)
                {
                    var request = new Secured { Name = "test" };
                    var response = client.Send<SecureResponse>(request);
                    Assert.That(response.Result, Is.EqualTo(request.Name));
                    Console.WriteLine("loop : {0}", i);
                }
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_false()
        {
            try
            {
                var client = (IRestClient)GetClientWithUserPassword();
                ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = false;
                var response = client.Get<SecuredResponse>("/secured");

                Assert.Fail("Should have thrown");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
            }
        }

        [Test]
        public void Exceptions_thrown_are_received_by_client_when_AlwaysSendBasicAuthHeader_is_true()
        {
            try
            {
                var client = (IRestClient)GetClientWithUserPassword();
                ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = true;
                var response = client.Get<SecuredResponse>("/secured");

                Assert.Fail("Should have thrown");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ErrorMessage, Is.EqualTo("unicorn nuggets"));
            }
        }

        [Test]
        public void Html_clients_receive_redirect_to_login_page_when_accessing_unauthenticated()
        {
            var client = (ServiceClientBase)GetHtmlClient();
            client.AllowAutoRedirect = false;
            string lastResponseLocationHeader = null;
            client.ResponseFilter = response =>
            {
                lastResponseLocationHeader = response.Headers["Location"];
            };

            var request = new Secured { Name = "test" };
            client.Send<SecureResponse>(request);

            var locationUri = new Uri(lastResponseLocationHeader);
            var loginPath = "/".CombineWith(VirtualDirectory).CombineWith(LoginUrl);
            Assert.That(locationUri.AbsolutePath, Is.EqualTo(loginPath).IgnoreCase);
        }

        [Test]
        public void Html_clients_receive_secured_url_attempt_in_login_page_redirect_query_string()
        {
            var client = (ServiceClientBase)GetHtmlClient();
            client.AllowAutoRedirect = false;
            string lastResponseLocationHeader = null;
            client.ResponseFilter = response =>
            {
                lastResponseLocationHeader = response.Headers["Location"];
            };

            var request = new Secured { Name = "test" };
            client.Send<SecureResponse>(request);

            var locationUri = new Uri(lastResponseLocationHeader);
            var queryString = HttpUtility.ParseQueryString(locationUri.Query);
            var redirectQueryString = queryString["redirect"];
            var redirectUri = new Uri(redirectQueryString);

            // Should contain the url attempted to access before the redirect to the login page.
            var securedPath = "/".CombineWith(VirtualDirectory).CombineWith("secured");
            Assert.That(redirectUri.AbsolutePath, Is.EqualTo(securedPath).IgnoreCase);
            // The url should also obey the WebHostUrl setting for the domain.
            var redirectSchemeAndHost = redirectUri.Scheme + "://" + redirectUri.Authority;
            var webHostUri = new Uri(WebHostUrl);
            var webHostSchemeAndHost = webHostUri.Scheme + "://" + webHostUri.Authority;
            Assert.That(redirectSchemeAndHost, Is.EqualTo(webHostSchemeAndHost).IgnoreCase);
        }

        [Test]
        public void Html_clients_receive_secured_url_including_query_string_within_login_page_redirect_query_string()
        {
            var client = (ServiceClientBase)GetHtmlClient();
            client.AllowAutoRedirect = false;
            string lastResponseLocationHeader = null;
            client.ResponseFilter = response =>
            {
                lastResponseLocationHeader = response.Headers["Location"];
            };

            var request = new Secured { Name = "test" };
            // Perform a GET so that the Name DTO field is encoded as query string.
            client.Get(request);

            var locationUri = new Uri(lastResponseLocationHeader);
            var locationUriQueryString = HttpUtility.ParseQueryString(locationUri.Query);
            var redirectQueryItem = locationUriQueryString["redirect"];
            var redirectUri = new Uri(redirectQueryItem);

            // Should contain the url attempted to access before the redirect to the login page,
            // including the 'Name=test' query string.
            var redirectUriQueryString = HttpUtility.ParseQueryString(redirectUri.Query);
            Assert.That(redirectUriQueryString.AllKeys, Contains.Item("name"));
            Assert.That(redirectUriQueryString["name"], Is.EqualTo("test"));
        }

        [Test]
        public void Html_clients_receive_session_ReferrerUrl_on_successful_authentication()
        {
            var client = (ServiceClientBase)GetHtmlClient();
            client.AllowAutoRedirect = false;
            string lastResponseLocationHeader = null;
            client.ResponseFilter = response =>
            {
                lastResponseLocationHeader = response.Headers["Location"];
            };

            client.Send(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = UserNameWithSessionRedirect,
                Password = PasswordForSessionRedirect,
                RememberMe = true,
            });

            Assert.That(lastResponseLocationHeader, Is.EqualTo(SessionRedirectUrl));
        }

        public void Already_authenticated_session_returns_correct_username()
        {
            var client = GetClient();

            var authRequest = new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = UserName,
                Password = Password,
                RememberMe = true,
            };
            var initialLoginResponse = client.Send(authRequest);
            var alreadyLogggedInResponse = client.Send(authRequest);

            Assert.That(alreadyLogggedInResponse.UserName, Is.EqualTo(UserName));
        }


        [Test]
        public void AuthResponse_returns_email_as_username_if_user_registered_with_email()
        {
            var client = GetClient();

            var authRequest = new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = EmailBasedUsername,
                Password = PasswordForEmailBasedAccount,
                RememberMe = true,
            };
            var authResponse = client.Send(authRequest);

            Assert.That(authResponse.UserName, Is.EqualTo(EmailBasedUsername));
        }

        [Test]
        public void Already_authenticated_session_returns_correct_username_when_user_registered_with_email()
        {
            var client = GetClient();

            var authRequest = new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = EmailBasedUsername,
                Password = PasswordForEmailBasedAccount,
                RememberMe = true,
            };
            var initialLoginResponse = client.Send(authRequest);
            var alreadyLogggedInResponse = client.Send(authRequest);

            Assert.That(initialLoginResponse.UserName, Is.EqualTo(EmailBasedUsername));
            Assert.That(alreadyLogggedInResponse.UserName, Is.EqualTo(EmailBasedUsername));
        }

        [Test]
        public void Can_call_RequiresAnyRole_service_with_BasicAuth()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var roles = new List<string>() {
                    "test", "test2"
                };
                var request = new RequiresAnyRole { Roles = roles };
                var response = client.Send<RequiresAnyRoleResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Roles));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void RequiresAnyRole_service_returns_unauthorized_if_no_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                var roles = new List<string>() {
                    "test", "test2"
                };
                var request = new RequiresAnyRole { Roles = roles };
                var response = client.Send<RequiresAnyRole>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void RequiresAnyRole_service_returns_forbidden_if_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                ((ServiceClientBase)client).UserName = EmailBasedUsername;
                ((ServiceClientBase)client).Password = PasswordForEmailBasedAccount;

                var roles = new List<string>() {
                    "test", "test2"
                };
                var request = new RequiresAnyRole { Roles = roles };
                var response = client.Send<RequiresAnyRoleResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void Can_call_RequiresAnyPermission_service_with_BasicAuth()
        {
            try
            {
                var client = GetClientWithUserPassword();
                var permissions = new List<string>
                {
                    "test", "test2"
                };
                var request = new RequiresAnyPermission { Permissions = permissions };
                var response = client.Send<RequiresAnyPermissionResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Permissions));
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void RequiresAnyPermission_service_returns_unauthorized_if_no_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                var permissions = new List<string>
                {
                    "test", "test2"
                };
                var request = new RequiresAnyPermission { Permissions = permissions };
                var response = client.Send<RequiresAnyPermissionResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void RequiresAnyPermission_service_returns_forbidden_if_basic_auth_header_exists()
        {
            try
            {
                var client = GetClient();
                ((ServiceClientBase)client).UserName = EmailBasedUsername;
                ((ServiceClientBase)client).Password = PasswordForEmailBasedAccount;
                var permissions = new List<string>
                {
                    "test", "test2"
                };
                var request = new RequiresAnyPermission { Permissions = permissions };
                var response = client.Send<RequiresAnyPermissionResponse>(request);
                Assert.Fail();
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
        }

        [Test]
        public void Calling_AddSessionIdToRequest_from_a_custom_auth_attribute_does_not_duplicate_session_cookies()
        {
            WebHeaderCollection headers = null;
            var client = GetClientWithUserPassword();
            ((ServiceClientBase)client).AlwaysSendBasicAuthHeader = true;
            ((ServiceClientBase)client).ResponseFilter = x => headers = x.Headers;
            var response = client.Send<CustomAuthAttrResponse>(new CustomAuthAttr() { Name = "Hi You" });
            Assert.That(response.Result, Is.EqualTo("Hi You"));
            Assert.That(
                System.Text.RegularExpressions.Regex.Matches(headers["Set-Cookie"], "ss-id=").Count,
                Is.EqualTo(1)
            );
        }


        [TestCase(ExpectedException = typeof(AuthenticationException))]
        public void Meaningful_Exception_for_Unknown_Auth_Header()
        {
            var authInfo = new AuthenticationInfo("Negotiate,NTLM");
        }

        [Test]
        public void Can_logout_using_CredentailsAuth()
        {
            Assert.That(AuthenticateService.LogoutAction, Is.EqualTo("logout"));

            try
            {
                var client = GetClient();

                var authResponse = client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "user",
                    Password = "p@55word",
                    RememberMe = true,
                });

                Assert.That(authResponse.SessionId, Is.Not.Null);

                var logoutResponse = client.Get<AuthenticateResponse>("/auth/logout");

                Assert.That(logoutResponse.ResponseStatus.ErrorCode, Is.Null);

                logoutResponse = client.Send(new Authenticate
                {
                    provider = AuthenticateService.LogoutAction,
                });

                Assert.That(logoutResponse.ResponseStatus.ErrorCode, Is.Null);
            }
            catch (WebServiceException webEx)
            {
                Assert.Fail(webEx.Message);
            }
        }

        [Test]
        public void WebSudoRequired_service_returns_PaymentRequired_if_not_re_authenticated()
        {
            try
            {
                var client = GetClient();
                var authRequest = new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = UserName,
                    Password = Password,
                    RememberMe = true,
                };
                client.Send(authRequest);
                var request = new RequiresWebSudo { Name = "test" };
                var response = client.Send(request);

                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.PaymentRequired));
                Console.WriteLine(webEx.Dump());
            }
        }

        [Test]
        public void WebSudoRequired_service_succeeds_if_re_authenticated()
        {
            var client = GetClient();
            var authRequest = new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = UserName,
                Password = Password,
                RememberMe = true,
            };
            client.Send(authRequest);

            var request = new RequiresWebSudo { Name = "test" };
            try
            {
                client.Send<RequiresWebSudoResponse>(request);
                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException)
            {
                client.Send(authRequest);
                var response = client.Send<RequiresWebSudoResponse>(request);
                Assert.That(response.Result, Is.EqualTo(request.Name));
            }
        }

        [Test]
        public void Failed_re_authentication_does_not_logout_user()
        {
            var client = GetClient();
            var authRequest = new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = UserName,
                Password = Password,
                RememberMe = true,
            };
            client.Send(authRequest);
            var request = new RequiresWebSudo { Name = "test" };
            try
            {
                client.Send(request);
                Assert.Fail("Shouldn't be allowed");
            }
            catch
            {
                // ignore the first 402
            }
            try
            {
                client.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = UserName,
                    Password = "some other password",
                    RememberMe = true,
                });
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Console.WriteLine(webEx.ResponseDto.Dump());
            }
            
            // Should still be authenticated, but not elevated
            try 
            { 
                client.Send<RequiresWebSudoResponse>(request);
                Assert.Fail("Shouldn't be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.PaymentRequired));
                Console.WriteLine(webEx.Dump());
            }
        }
    }

    public class AuthTestsWithinVirtualDirectory : AuthTests
    {
        protected override string VirtualDirectory { get { return "somevirtualdirectory"; } }
        protected override string ListeningOn { get { return "http://localhost:1337/" + VirtualDirectory + "/"; } }
        protected override string WebHostUrl { get { return "http://mydomain.com/" + VirtualDirectory; } }
    }

    public class AuthTestsWithinOrmLiteCache : AuthTests
    {
        protected override string VirtualDirectory { get { return "somevirtualdirectory"; } }
        protected override string ListeningOn { get { return "http://localhost:1337/" + VirtualDirectory + "/"; } }
        protected override string WebHostUrl { get { return "http://mydomain.com/" + VirtualDirectory; } }

        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
            container.Resolve<ICacheClient>().InitSchema();
        }
    }
}
