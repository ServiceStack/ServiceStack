using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class SomeResponse
{
    public string Info { get; set; }
}

public class InternalResponse : SomeResponse
{
}

public class RequestSync : IReturn<SomeResponse> { }
public class RequestAsync : IReturn<SomeResponse> { }
public class RequestInternal: IGet, IReturn<InternalResponse> { }
public class PriorToValidatedRequest : IReturn<ValidatedResponse>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class ValidatedRequest : IReturn<ValidatedResponse>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class ValidatedResponse : IHasResponseStatus
{
    public ResponseStatus ResponseStatus { get; set; }
}

public class ValidatedRequestValidator : AbstractValidator<ValidatedRequest>
{
    public ValidatedRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("FirstName is required");
        
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("LastName is required")
            .WithSeverity(Severity.Warning);
    }
}

public class FooBarService: Service
{
    public SomeResponse Get(RequestSync request)
    {
        var resp = Gateway.Send(new RequestInternal());
        return new SomeResponse() {Info = resp.Info};
    }

    public async Task<SomeResponse> Get(RequestAsync req)
    {
        var resp = await Gateway.SendAsync(new RequestInternal());
        return new SomeResponse() {Info = resp.Info};
    }
    
    public Task<InternalResponse> Get(RequestInternal req) => Task.FromResult(new InternalResponse() {Info = "yay"});

    public object Get(PriorToValidatedRequest request)
    {
        return Gateway.Send(request.ConvertTo<ValidatedRequest>());
    }
    
    public object Get(ValidatedRequest request)
    {
        return new ValidatedResponse();
    }
}

public class InProcessServiceGatewayRequestResponseFiltersTests
{
    sealed class InProcessAppHost() : AppSelfHostBase(nameof(InProcessServiceGatewayRequestResponseFiltersTests),
        typeof(ServiceGatewayServices).Assembly)
    {
        public override void Configure(Container container)
        {
            HostContext.AppHost.GetPlugin<ValidationFeature>().TreatInfoAndWarningsAsErrors = false;
        }
    }

    private readonly ServiceStackHost _appHost;        
    private readonly List<string> _filterCallLog = [];
    private readonly JsonServiceClient _client;

    public InProcessServiceGatewayRequestResponseFiltersTests()
    {
        _appHost = new InProcessAppHost();
        _appHost.GlobalRequestFilters.Add((req ,resp, dto) => _filterCallLog.Add(req.PathInfo));
        _appHost.GlobalResponseFilters.Add((req, resp, dto) => _filterCallLog.Add(dto.GetType().Name));

        _appHost.Init()
            .Start(Config.ListeningOn);
        _client = new JsonServiceClient(Config.ListeningOn);
    }

    [TearDown]
    public void CleanAfterTest()
    {
        _filterCallLog.Clear();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        _appHost.Dispose();
    }

    [Test]
    public void Should_Not_Call_Filters_When_Using_SyncGateway()
    {
        var result = _client.Get(new RequestSync());
        Assert.AreEqual("yay", result.Info);            
        CollectionAssert.AreEqual(new []{ "/json/reply/RequestSync", "SomeResponse" }, _filterCallLog);
    }

    [Test]
    public void Should_Not_Call_Filters_When_Using_AsyncGateway()
    {
        var result = _client.Get(new RequestAsync());
        Assert.AreEqual("yay", result.Info);
        CollectionAssert.AreEqual(new[] { "/json/reply/RequestAsync", "SomeResponse" }, _filterCallLog);
    }
    
    [Test]
    public void Should_Be_Valid_When_Called_With_Populated_Properties()
    {
        var result = _client.Get(new PriorToValidatedRequest
        {
            FirstName = "Service",
            LastName = "Stack"
        });
        Assert.That(result.ResponseStatus, Is.Null);
    }
    
    [Test]
    public void Should_Fail_When_Required_Property_Empty()
    {
        Assert.Throws<WebServiceException>(() =>
        { 
            _client.Get(new PriorToValidatedRequest
            {
                FirstName = null,
                LastName = "Stack"
            });
        });
    }
    
    [Test]
    public void Should_Have_Severity_As_Warning_When_TreatInfoAndWarningsAsErrors_Is_False()
    {
        var result = _client.Get(new PriorToValidatedRequest
        {
            FirstName = "Service",
            LastName = null
        });
        
        Assert.That(result.ResponseStatus, Is.Not.Null);
        Assert.True(result.ResponseStatus.Errors!.First().Meta!.ContainsKey("Severity"));
        Assert.That(result.ResponseStatus.Errors!.First().Meta!["Severity"], Is.EqualTo("Warning"));
    }
}