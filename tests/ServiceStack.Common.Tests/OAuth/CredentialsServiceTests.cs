using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Common.Tests.OAuth
{
	[TestFixture]
	public class CredentialsServiceTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			AuthService.Init(() => new AuthUserSession(),
				new CredentialsAuthProvider());           
		}

		public AuthService GetAuthService()
		{
		    var authService = new AuthService {
                RequestContext = new MockRequestContext(),
                //ServiceExceptionHandler = (req, ex) =>
                //    ValidationFeature.HandleException(new BasicResolver(), req, ex)
            };
		    return authService;
		}

        class ValidateServiceRunner<T> : ServiceRunner<T>
        {
            public ValidateServiceRunner(IAppHost appHost, ActionContext actionContext)
                : base(appHost, actionContext) {}

            public override object HandleException(IRequestContext requestContext, T request, System.Exception ex)
            {
                return DtoUtils.HandleException(new BasicResolver(), request, ex);
            }
        }

        public object GetAuthService(AuthService authService, Auth request)
        {
            var serviceRunner = new ValidateServiceRunner<Auth>(null, new ActionContext {
                Id = "GET Auth",
                ServiceAction = (service, req) => ((AuthService)service).Get((Auth)req)
            });

            return serviceRunner.Process(authService.RequestContext, authService, request);
        }

	    [Test]
		public void Empty_request_invalidates_all_fields()
		{
			var authService = GetAuthService();

            var response = (HttpError)GetAuthService(authService, new Auth());
			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(2));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
		}

		[Test]
		public void Requires_UserName_and_Password()
		{
			var authService = GetAuthService();

            var response = (HttpError)GetAuthService(authService,
                new Auth { provider = AuthService.CredentialsProvider });

			var errors = response.GetFieldErrors();

			Assert.That(errors.Count, Is.EqualTo(2));
			Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
			Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
			Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
			Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
		}
	}
}