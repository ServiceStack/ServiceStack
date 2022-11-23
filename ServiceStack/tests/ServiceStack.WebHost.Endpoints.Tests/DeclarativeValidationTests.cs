using Funq;
using NUnit.Framework;
using System.Collections.Generic;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class DeclarativeChildValidation
    {
        public string Name { get; set; }
        [ValidateMaximumLength(20)]
        public string Value { get; set; }
    } 

    public class FluentChildValidation
    {
        public string Name { get; set; }
        public string Value { get; set; }
    } 

    public class DeclarativeCollectiveValidationTest : IReturn<EmptyResponse>
    {
        [ValidateNotEmpty]
        [ValidateMaximumLength(20)]
        public string Site { get; set; }
        public List<DeclarativeChildValidation> DeclarativeValidations { get; set; }
        public List<FluentChildValidation> FluentValidations { get; set; }
        public List<NoValidators> NoValidators { get; set; }
    }

    public class DeclarativeCollectionValidationTest2 : IReturn<EmptyResponse>
    {
        [ValidateNotEmpty]
        [ValidateMaximumLength(20)]
        public string Site { get; set; }
        [ValidateNotEmpty]
        public List<DeclarativeChildValidation> DeclarativeValidationsWithNotEmpty { get; set; }
    }

    public class DeclarativeSingleValidation
    {
        public string Name { get; set; }
        [ValidateMaximumLength(20)]
        public string Value { get; set; }
    } 

    public class FluentSingleValidation
    {
        public string Name { get; set; }
        public string Value { get; set; }
    } 

    public class DeclarativeSingleValidationTest : IReturn<EmptyResponse>
    {
        [ValidateNotEmpty]
        [ValidateMaximumLength(20)]
        public string Site { get; set; }
        public DeclarativeSingleValidation DeclarativeSingleValidation { get; set; }
        public FluentSingleValidation FluentSingleValidation { get; set; }
        public NoValidators NoValidators { get; set; }
    }

    public class NoValidators
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    // Declarative Collection Validation equivalent to:
    // public class DeclarativeValidationTestValidator : AbstractValidator<DeclarativeCollectiveValidationTest>
    // {
    //     public DeclarativeValidationTestValidator()
    //     {
    //         RuleForEach(x => x.FluentValidations).SetValidator(new CustomChildValidator());
    //     }
    // }
    public class FluentChildValidationValidator : AbstractValidator<FluentChildValidation>
    {
        public FluentChildValidationValidator()
        {
            RuleFor(x => x.Value).MaximumLength(20);
        }
    }

    // public class DeclarativeSingleValidationTestValidator : AbstractValidator<DeclarativeSingleValidationTest>
    // {
    //     public DeclarativeSingleValidationTestValidator()
    //     {
    //         RuleFor(x => x.FluentSingleValidation).SetValidator(new FluentSingleValidationValidator());
    //     }
    // }
    public class FluentSingleValidationValidator : AbstractValidator<FluentSingleValidation>
    {
        public FluentSingleValidationValidator()
        {
            RuleFor(x => x.Value).MaximumLength(20);
        }
    }

    public class DeclarativeValidationTestUpdate : DeclarativeCollectiveValidationTest, IReturn<DeclarativeCollectiveValidationTest> { }

    public class DeclarativeValidationServices : Service
    {
        public object Any(DeclarativeCollectiveValidationTest request)
        {
            return new EmptyResponse();
        }
        public object Any(DeclarativeSingleValidationTest request)
        {
            return new EmptyResponse();
        }

        public object Any(DeclarativeCollectionValidationTest2 request) => new EmptyResponse();

    }
    
    public class DeclarativeValidationTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(DeclarativeValidationTests), typeof(DeclarativeValidationServices)) {}
            
            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());
                
                container.RegisterValidator(typeof(FluentChildValidationValidator));
                container.RegisterValidator(typeof(FluentSingleValidationValidator));
            }
        }

        private readonly ServiceStackHost appHost;
        public DeclarativeValidationTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }
        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();
        IServiceClient CreateClient() => new JsonServiceClient(Config.ListeningOn);

        [Test]
        public void Does_execute_declarative_collection_validation_with_not_empty()
        {
            var client = CreateClient();

            try
            {
                var invalidRequest = new DeclarativeCollectionValidationTest2 {
                    Site = "Location 1",
                    DeclarativeValidationsWithNotEmpty = new List<DeclarativeChildValidation>
                    {
                        new() { Name = "Location 1", Value = "Very long description > 20 chars" }
                    }
                };
                var response = client.Post(invalidRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                var status = ex.GetResponseStatus();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("DeclarativeValidationsWithNotEmpty[0].Value"));
            }
        }
        
        [Test]
        public void Does_execute_declarative_collection_validation_for_declarative_collections()
        {
            var client = CreateClient();

            try
            {
                var invalidRequest = new DeclarativeCollectiveValidationTest {
                    Site = "Location 1",
                    DeclarativeValidations = new List<DeclarativeChildValidation> {
                        new() { Name = "Location 1", Value = "Very long description > 20 chars" }
                    }
                };
                var response = client.Post(invalidRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                var status = ex.GetResponseStatus();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("DeclarativeValidations[0].Value"));
            }
        }

        [Test]
        public void Does_execute_declarative_collection_validation_for_FluentValidation_collections()
        {
            var client = CreateClient();

            try
            {
                var invalidRequest = new DeclarativeCollectiveValidationTest {
                    Site = "Location 1",
                    FluentValidations = new List<FluentChildValidation> {
                        new() {Name = "Location 1", Value = "Very long description > 20 chars"}
                    }
                };
                var response = client.Post(invalidRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                var status = ex.GetResponseStatus();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("FluentValidations[0].Value"));
            }
        }
 
        [Test]
        public void Does_execute_declarative_single_validation_for_DeclarativeSingleValidation()
        {
            var client = CreateClient();

            try
            {
                var invalidRequest = new DeclarativeSingleValidationTest {
                    Site = "Location 1",
                    DeclarativeSingleValidation = new() { Name = "Location 1", Value = "Very long description > 20 chars" },
                };
                var response = client.Post(invalidRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                var status = ex.GetResponseStatus();
                status.PrintDump();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("DeclarativeSingleValidation.Value"));
            }
        }
 
        [Test]
        public void Does_execute_declarative_single_validation_for_FluentSingleValidation()
        {
            var client = CreateClient();

            try
            {
                var invalidRequest = new DeclarativeSingleValidationTest {
                    Site = "Location 1",
                    FluentSingleValidation = new() { Name = "Location 1", Value = "Very long description > 20 chars" },
                };
                var response = client.Post(invalidRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                var status = ex.GetResponseStatus();
                status.PrintDump();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo(nameof(ValidateScripts.MaximumLength)));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("FluentSingleValidation.Value"));
            }
        }        
    }
}