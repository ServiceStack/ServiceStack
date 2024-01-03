using System;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MyRegister : IReturn<EmptyResponse>
{
    public string Email { get; set; }
}
    
public class MyRegisterValidator : AbstractValidator<MyRegister>
{
    public MyRegisterValidator()
    {
        RuleSet(ApplyTo.Get | ApplyTo.Post | ApplyTo.Put,
            () =>
            {
                RuleFor(x => x.Email).EmailAddress();
            });
    }
}
    
public class MyRegisterService : Service
{
    public object Get(MyRegister request)
    {
        var validator = new MyRegisterValidator { Request = Request };
        var validationResult = validator.Validate(Request, request);
        if (!validationResult.IsValid)
            throw validationResult.ToException();
        return new EmptyResponse();
    }
        
    public async Task<object> Post(MyRegister request)
    {
        var validator = new MyRegisterValidator { Request = Request };
        var validationResult = await validator.ValidateAsync(Request, request);
        if (!validationResult.IsValid)
            throw validationResult.ToException();
        return new EmptyResponse();
    }
        
    public object Put(MyRegister request)
    {
        var validator = new MyRegisterValidator { Request = Request };
        var validationResult = validator.Validate(Request, request);
        if (!validationResult.IsValid)
            throw validationResult.ToException();
        return new EmptyResponse();
    }
}    

public class ManualValidationTests
{
    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(ManualValidationTests), typeof(MyRegisterService).Assembly) {}
            
        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature {
                ScanAppHostAssemblies = false,
            });
        }
    }

    private readonly ServiceStackHost appHost;
    public ManualValidationTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();
        
    JsonServiceClient CreateClient() => new JsonServiceClient(Config.ListeningOn);

    private static void AssertMyRegisterManualValidation(WebServiceException e)
    {
        Assert.That(e.ErrorCode, Is.EqualTo(nameof(MyRegister.Email)));
        Assert.That(e.Message, Is.EqualTo("'Email' is not a valid email address."));
        Assert.That(e.ResponseStatus.Errors[0].ErrorCode, Is.EqualTo(nameof(MyRegister.Email)));
        Assert.That(e.ResponseStatus.Errors[0].FieldName, Is.EqualTo(nameof(MyRegister.Email)));
        Assert.That(e.ResponseStatus.Errors[0].Message, Is.EqualTo("'Email' is not a valid email address."));
    }

    [Test]
    public void Can_manual_validate_sync_Get_Validate()
    {
        var client = CreateClient();

        try
        {
            client.Get(new MyRegister { Email = "not.an.email" });
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            AssertMyRegisterManualValidation(e);
        }
    }

    [Test]
    public async Task Can_manual_validate_async_Post_ValidateAsync()
    {
        var client = CreateClient();

        try
        {
            await client.PostAsync(new MyRegister { Email = "not.an.email" });
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            AssertMyRegisterManualValidation(e);
        }
    }

    [Test]
    public async Task Can_manual_validate_async_Put_Validate()
    {
        var client = CreateClient();

        try
        {
            await client.PutAsync(new MyRegister { Email = "not.an.email" });
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            AssertMyRegisterManualValidation(e);
        }
    }
}