using Funq;
using NUnit.Framework;
using System.Collections.Generic;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class Location
    {
        public string Name { get; set; }
        [ValidateMaximumLength(20)]
        public string Value { get; set; }
    } 

    public class DeclarativeValidationTest : IReturn<EmptyResponse>
    {
        [ValidateNotEmpty]
        [ValidateMaximumLength(20)]
        public string Site { get; set; }
        public List<Location> Locations { get; set; } // **** here's the example
        public List<NoValidators> NoValidators { get; set; }
    }

    public class NoValidators
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    // Declarative Collection Validation equivalent to:
    // public class DeclarativeValidationTestValidator : AbstractValidator<DeclarativeValidationTest>
    // {
    //     public DeclarativeValidationTestValidator()
    //     {
    //         RuleForEach(x => x.Locations).SetValidator(new LocationValidator());
    //     }
    // }
    // public class LocationValidator : AbstractValidator<Location>
    // {
    //     public LocationValidator()
    //     {
    //         RuleFor(x => x.Value).MaximumLength(20);
    //     }
    // }

    public class DeclarativeValidationTestUpdate : DeclarativeValidationTest, IReturn<DeclarativeValidationTest> { }

    public class DeclarativeValidationServices : Service
    {
        public object Any(DeclarativeValidationTest request)
        {
            return new EmptyResponse();
        }
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

        public DeclarativeValidationTest InvalidCollectionRequest() => new() {
            Site = "Location 1",
            Locations = new List<Location> {
                new() { Name = "Location 1", Value = "Very long description > 20 chars" }
            }
        };

        [Test]
        public void Does_execute_declarative_collection_validation_for_List_collections()
        {
            var client = CreateClient();

            try
            {
                var response = client.Post(InvalidCollectionRequest());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo("MaximumLength"));
                var status = ex.GetResponseStatus();
                // status.PrintDump();

                var errorMsg = "The length of 'Value' must be 20 characters or fewer. You entered 32 characters.";
                Assert.That(status.ErrorCode, Is.EqualTo("MaximumLength"));
                Assert.That(status.Message, Is.EqualTo(errorMsg));
                var errors = status.Errors;
                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].ErrorCode, Is.EqualTo("MaximumLength"));
                Assert.That(errors[0].Message, Is.EqualTo(errorMsg));
                Assert.That(errors[0].FieldName, Is.EqualTo("Locations[0].Value"));
            }
        }
    }
}