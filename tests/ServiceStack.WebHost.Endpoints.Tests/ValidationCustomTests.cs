using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/validation/custom")]
    public class CustomValidation
    {
        public string Name { get; set; }
    }

    public class CustomValidationResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CustomValidationValidator : AbstractValidator<CustomValidation>
    {
        public CustomValidationValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
            RuleFor(request => request)
                .Custom((request, contex) => {
                    if (request.Name?.StartsWith("A") != true)
                    {
                        var propertyName = contex.ParentContext.PropertyChain.BuildPropertyName($"Name:0");
                        var errorMessage = $"Incorrect prefix.";
                        var failure = new ValidationFailure(propertyName, errorMessage)
                        {
                            ErrorCode = "NotFound"
                        };
                        contex.AddFailure(failure);
                    }
                    var nameLength = request.Name?.Length ?? 0;
                    var firstLetter = request.Name?.Substring(0, 1) ?? "";
                    var lastLetter = request.Name?.Substring(nameLength - 1, 1) ?? "";
                    if (firstLetter != lastLetter)
                    {
                        var propertyName = contex.ParentContext.PropertyChain.BuildPropertyName($"Name:0:1 <> Name:{nameLength - 1}:{nameLength}");
                        var errorMessage = $"Name inconsistency: {firstLetter} <> {lastLetter}";
                        var failure = new ValidationFailure(propertyName, errorMessage)
                        {
                            ErrorCode = "Inconsistency"
                        };
                        contex.AddFailure(failure);
                    }
                });
        }
    }

    public class CustomValidationService : Service
    {
        public object Any(CustomValidation request)
        {
            return new CustomValidationResponse { Result = "Hello, " + request.Name };
        }
    }

    public class ValidationCustomTests
    {
        private readonly ServiceStackHost appHost;
        public class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ValidationCustomTests), typeof(HelloService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new ValidationFeature());
            }
        }

        public ValidationCustomTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_execute_custom_validators()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            try
            {
                var response = client.Get(new CustomValidation { Name = "Joan" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                var status = ex.GetResponseStatus();

                Assert.That(status.ErrorCode, Is.EqualTo("NotFound"));
                Assert.That(status.Message, Is.EqualTo("Incorrect prefix."));
                Assert.That(status.Errors.Count, Is.EqualTo(2));

                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotFound"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name:0"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("Incorrect prefix."));

                Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("Inconsistency"));
                Assert.That(status.Errors[1].FieldName, Is.EqualTo("Name:0:1 <> Name:3:4"));
                Assert.That(status.Errors[1].Message, Is.EqualTo("Name inconsistency: J <> n"));
            }
        }

        [Test]
        public void Does_execute_custom_validators_combined()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            try
            {
                var response = client.Get(new CustomValidation());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                var status = ex.GetResponseStatus();

                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Name' should not be empty."));
                Assert.That(status.Errors.Count, Is.EqualTo(2));

                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("NotFound"));
                Assert.That(status.Errors[1].FieldName, Is.EqualTo("Name:0"));
                Assert.That(status.Errors[1].Message, Is.EqualTo("Incorrect prefix."));
            }
        }

    }
}
