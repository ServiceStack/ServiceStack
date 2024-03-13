using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests;

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
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithState(x => new Dictionary<string,string> { ["Custom"] = "Dictionary" });
        RuleFor(request => request)
            .Custom((request, context) => {
                if (request.Name?.StartsWith("A") != true)
                {
                    var propertyName = context.ParentContext.PropertyChain.BuildPropertyName("Name:0");
                    var errorMessage = "Incorrect prefix.";
                    var failure = new ValidationFailure(propertyName, errorMessage)
                    {
                        ErrorCode = "NotFound"
                    };
                    context.AddFailure(failure);
                }
                var nameLength = request.Name?.Length ?? 0;
                var firstLetter = request.Name?.Substring(0, 1) ?? "";
                var lastLetter = request.Name?.Substring(nameLength - 1, 1) ?? "";
                if (firstLetter != lastLetter)
                {
                    var propertyName = context.ParentContext.PropertyChain.BuildPropertyName($"Name:0:1 <> Name:{nameLength - 1}:{nameLength}");
                    var errorMessage = $"Name inconsistency: {firstLetter} <> {lastLetter}";
                    var failure = new ValidationFailure(propertyName, errorMessage)
                    {
                        ErrorCode = "Inconsistency"
                    };
                    context.AddFailure(failure);
                }
            });
    }
}

[Route("/validation/inline")]
public class InlineValidation : IReturn<InlineValidation>
{
}

public class InlineModel
{
    public string Name { get; set; }
}
public class InlineModelValidator : AbstractValidator<InlineModel>
{
    public InlineModelValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithState(x => new { Custom = "State" });
    }
}


public class ListEntry
{
    public required string Prop1 { get; set; }

    public required int Prop2 { get; set; }

    public required ListEntryChild Child { get; set; }
}

public class ListEntryValidator : AbstractValidator<ListEntry>
{
    public ListEntryValidator()
    {
        RuleFor(h => h.Prop1).NotEmpty().WithMessage("Prop1 cannot be empty!");

        RuleFor(h => h.Prop2).Must(t => t >= 10).WithMessage("Prop2 must be at least 10!");

        RuleFor(h => h.Child).NotEmpty().WithMessage("Child cannot be empty!");
    }
}

public class ListEntryChild
{
    [ValidateNotEmpty]
    public required string ChildProp { get; set; }
}

public class CustomValidationService : Service
{
    public object Any(CustomValidation request)
    {
        return new CustomValidationResponse { Result = "Hello, " + request.Name };
    }

    public object Any(InlineValidation request)
    {
        var validationResult = new InlineModelValidator().Validate(new InlineModel());
        if (!validationResult.IsValid)
            throw validationResult.ToException();

        return request;
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
            using var response = client.Get<HttpWebResponse>(new CustomValidation {Name = "Joan"});
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
            using var response = client.Get<HttpWebResponse>(new CustomValidation());
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            var status = ex.GetResponseStatus();

            Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(status.Message, Is.EqualTo("'Name' must not be empty."));
            Assert.That(status.Errors.Count, Is.EqualTo(2));

            Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
            Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' must not be empty."));
            Assert.That(status.Errors[0].Meta["Custom"], Is.EqualTo("Dictionary"));

            Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("NotFound"));
            Assert.That(status.Errors[1].FieldName, Is.EqualTo("Name:0"));
            Assert.That(status.Errors[1].Message, Is.EqualTo("Incorrect prefix."));
        }
    }

    [Test]
    public void Does_include_CustomState_for_inline_validation()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        try
        {
            var response = client.Get(new InlineValidation());
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            var status = ex.GetResponseStatus();
            status.PrintDump();

            Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(status.Message, Is.EqualTo("'Name' must not be empty."));
            Assert.That(status.Errors.Count, Is.EqualTo(1));

            Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
            Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' must not be empty."));
            Assert.That(status.Errors[0].Meta["Custom"], Is.EqualTo("State"));
        }
    }

}