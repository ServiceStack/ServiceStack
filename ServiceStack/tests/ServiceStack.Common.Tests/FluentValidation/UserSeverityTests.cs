// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/. 

using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.FluentValidation
{
    namespace ServiceStack.FluentValidation.Tests
    {
        using System;
        using System.Linq;
        using NUnit.Framework;
        using ServiceStack.FluentValidation;

        public class UserSeverityTests
        {
            private const string Urlbase = "http://localhost:20000/";

            [Test]
            public void Stores_user_severity_against_validation_failure()
            {
                var validator = new TestValidator();
                validator.RuleFor(x => x.Lastname).NotNull().WithSeverity(Severity.Info);
                var result = validator.Validate(new ErrorCodeTests.Person());
                Assert.AreEqual(Severity.Info, result.Errors.Single().Severity);
            }

            [Test]
            public void Defaults_user_severity_to_error()
            {
                var validator = new TestValidator();
                validator.RuleFor(x => x.Lastname).NotNull();
                var result = validator.Validate(new ErrorCodeTests.Person());
                Assert.AreEqual(Severity.Error, result.Errors.Single().Severity);
            }

            public class TestValidator : AbstractValidator<ErrorCodeTests.Person>
            {
            }

            [Test]
            public void Response_returned_when_valid()
            {
                using (var appHost = new TestAppHost())
                {
                    appHost.Init();
                    appHost.Start(Urlbase);

                    var sc = new JsonServiceClient(Urlbase);

                    var response = sc.Get(new EchoRequest { Day = "Monday", Word = "Word" });

                    Assert.That(response.Day, Is.EqualTo("Monday"));
                    Assert.That(response.Word, Is.EqualTo("Word"));
                }
            }

            [Test]
            public void Can_treat_warnings_and_info_as_errors()
            {
                using (var appHost = new TestAppHost())
                {
                    appHost.ConfigurePlugin<ValidationFeature>(x => x.TreatInfoAndWarningsAsErrors = true);
                    appHost.Init();
                    appHost.Start(Urlbase);

                    var sc = new JsonServiceClient(Urlbase);

                    Assert.Throws<WebServiceException>(() => sc.Get(new EchoRequest { Day = "Monday", Word = "" }),
                        "'Word' should not be empty.");
                }
            }

            [Test]
            public void Can_return_response_when_no_failed_validations_and_TreatInfoAndWarningsAsErrors_set_false()
            {
                using (var appHost = new TestAppHost())
                {
                    appHost.ConfigurePlugin<ValidationFeature>(x => x.TreatInfoAndWarningsAsErrors = false);
                    appHost.Init();
                    appHost.Start(Urlbase);

                    var sc = new JsonServiceClient(Urlbase);

                    var resp = sc.Get(new EchoRequest { Day = "Monday", Word = "Word" });

                    Assert.That(resp.ResponseStatus, Is.Null);
                }
            }

            [Test]
            public void Can_ignore_warnings_and_info_as_errors()
            {
                using (var appHost = new TestAppHost())
                {
                    appHost.ConfigurePlugin<ValidationFeature>(x => x.TreatInfoAndWarningsAsErrors = false);
                    appHost.Init();
                    appHost.Start(Urlbase);

                    var sc = new JsonServiceClient(Urlbase);

                    var response = sc.Get(new EchoRequest { Day = "", Word = "" });

                    Assert.That(response.ResponseStatus, Is.Not.Null);
                    Assert.That(response.ResponseStatus.Errors, Is.Not.Empty);
                    Assert.That(response.ResponseStatus.Errors.First().Meta["Severity"], Is.EqualTo("Info"));
                    Assert.That(response.ResponseStatus.Errors[1].Meta["Severity"], Is.EqualTo("Warning"));
                }
            }

            internal class TestAppHost : AppSelfHostBase
            {
                public Action<Container> ConfigureContainer { get; set; }

                public TestAppHost()
                    : base("AppHost for ValidationFeature SeverityTests", typeof(EchoService).Assembly)
                {
                }

                public override void Configure(Container container)
                {
                    if (ConfigureContainer != null)
                        this.ConfigureContainer(container);
                }
            }
        }

        public class EchoService : Service
        {
            public object Any(EchoRequest request) => new EchoResponse { Day = request.Day, Word = request.Word };
        }

        public class EchoRequestValidator : AbstractValidator<EchoRequest>
        {
            public EchoRequestValidator()
            {
                RuleFor(e => e.Word).NotEmpty().WithSeverity(Severity.Info);
                RuleFor(e => e.Day).NotEmpty().WithSeverity(Severity.Warning);
            }
        }

        public class EchoRequest : IReturn<EchoResponse>
        {
            public string Word { get; set; }
            public string Day { get; set; }
        }

        public class EchoResponse : IHasResponseStatus
        {
            public string Word { get; set; }
            public string Day { get; set; }
            public ResponseStatus ResponseStatus { get; set; }
        }
    }
}