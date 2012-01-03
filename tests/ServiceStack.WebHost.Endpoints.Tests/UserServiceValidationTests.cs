﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using NUnit.Framework;
using ServiceStack.ServiceInterface.Validation;
using System.Collections;
using Funq;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [RestService("/users")]
    [RestService("/users/{Id}")]
    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public interface IAddressValidator
    {
        bool ValidAddress(string address);
    }

    public class UserValidator : AbstractValidator<User>
    {
        public IAddressValidator AddressValidator { get; set; }

        public UserValidator()
        {
            RuleSet(ApplyTo.Post | ApplyTo.Put, () =>
            {
                RuleFor(x => x.LastName).NotEmpty().WithErrorCode("ShouldNotBeEmpty");
                RuleFor(x => x.FirstName).NotEmpty().WithMessage("Please specify a first name");
            });
        }
    }

    //Not matching the naming convention ([Request DTO Name] + "Response")
    public class OperationResponse
    {
        public User Result { get; set; }
    }

    public class UserService : RestServiceBase<User>
    {
        public override object OnGet(User request)
        {
            return new OperationResponse { Result = request };
        }
    }

    [TestFixture]
    public class UserServiceValidationTests
    {
        private const string ListeningOn = "http://localhost:82/";

        public class UserAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public UserAppHostHttpListener()
                : base("Validation Tests", typeof(UserService).Assembly) { }

            public override void Configure(Container container)
            {
                ValidationFeature.Init(this);
                container.RegisterValidators(typeof(UserValidator).Assembly);
            }
        }

        UserAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new UserAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private static string ExpectedErrorCode = "ShouldNotBeEmpty";

        protected static IServiceClient UnitTestServiceClient()
        {
            EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(SecureService).Assembly);
            return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
        }

        public static IEnumerable ServiceClients
        {
            get
            {
                //Seriously retarded workaround for some devs idea who thought this should
                //be run for all test fixtures, not just this one.

                return new Func<IServiceClient>[] {
					() => UnitTestServiceClient(),
					() => new JsonServiceClient(ListeningOn),
					() => new JsvServiceClient(ListeningOn),
					() => new XmlServiceClient(ListeningOn),
				};
            }
        }

        [Test, TestCaseSource(typeof(UserServiceValidationTests), "ServiceClients")]
        public void Get_empty_request_throws_validation_exception(Func<IServiceClient> factory)
        {
            try
            {
                var client = (IRestClient)factory();
                var response = client.Get<OperationResponse>("Customers");
                Assert.Fail("Should throw Validation Exception");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));

                //One of this two properties should match
                Assert.That(ex.ErrorCode, Is.EqualTo(ExpectedErrorCode));
                Assert.That(ex.StatusDescription, Is.EqualTo(ExpectedErrorCode));
            }
        }

    }
}
