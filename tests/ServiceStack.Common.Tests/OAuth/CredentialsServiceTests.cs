#if !NETCORE_SUPPORT
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests.OAuth
{
    [TestFixture]
    public class CredentialsServiceTests
    {
        public class CredentialsTestAppHost : BasicAppHost
        {
            public CredentialsTestAppHost() : base(typeof(CredentialsServiceTests).Assembly) {}

            public override void Configure(Container container)
            {
                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(),
                    }));
            }

            public override IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
            {
                return new ValidateServiceRunner<TRequest>(this, actionContext);
            }
        }

        class ValidateServiceRunner<T> : ServiceRunner<T>
        {
            public ValidateServiceRunner(IAppHost appHost, ActionContext actionContext)
                : base(appHost, actionContext) { }

            public override Task<object> HandleExceptionAsync(IRequest request, T requestDto, System.Exception ex)
            {
                return DtoUtils.CreateErrorResponse(requestDto, ex).InTask();
            }
        }

        [Test]
        public void Empty_request_invalidates_all_fields()
		{
            using (var appHost = new CredentialsTestAppHost().Init())
            {
                var response = (HttpError)appHost.ExecuteService(
                    new Authenticate { provider = CredentialsAuthProvider.Name });

                var errors = response.GetFieldErrors();

                Assert.That(errors.Count, Is.EqualTo(2));
                Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
                Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
            }
		}

        [Test]
        public void Requires_UserName_and_Password()
        {
            using (var appHost = new CredentialsTestAppHost().Init())
            {
                var response = (HttpError)appHost.ExecuteService(
                    new Authenticate { provider = AuthenticateService.CredentialsProvider });

                var errors = response.GetFieldErrors();

                Assert.That(errors.Count, Is.EqualTo(2));
                Assert.That(errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(errors[0].FieldName, Is.EqualTo("UserName"));
                Assert.That(errors[1].FieldName, Is.EqualTo("Password"));
                Assert.That(errors[1].ErrorCode, Is.EqualTo("NotEmpty"));
            }
        }
    }
}
#endif