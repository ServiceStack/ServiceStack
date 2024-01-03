using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests;

public abstract class AsyncTaskTests
{
    private ServiceStackHost appHost;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new AsyncTaskAppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    protected abstract IServiceClient CreateServiceClient();

    private const int Param = 3;

    public class AsyncTaskAppHost()
        : AppHostHttpListenerBase(nameof(AsyncTaskAppHost), typeof(AsyncTaskAppHost).Assembly)
    {
        public override void Configure(Container container) {}
    }

    [Test]
    public void GetSync_GetFactorialGenericSync()
    {
        var response = CreateServiceClient().Get(new GetFactorialSync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public void GetSync_GetFactorialGenericAsync()
    {
        var response = CreateServiceClient().Get(new GetFactorialGenericAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public void GetSync_GetFactorialObjectAsync()
    {
        var response = CreateServiceClient().Get(new GetFactorialObjectAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public void GetSync_GetFactorialAwaitAsync()
    {
        var response = CreateServiceClient().Get(new GetFactorialAwaitAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public void GetSync_GetFactorialDelayAsync()
    {
        var response = CreateServiceClient().Get(new GetFactorialDelayAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }


    [Test]
    public async Task GetAsync_GetFactorialGenericSync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialSync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialGenericAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialGenericAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialObjectAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialObjectAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialAwaitAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialAwaitAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialDelayAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialDelayAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialNewTaskAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialNewTaskAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_GetFactorialNewTcsAsync()
    {
        var response = await CreateServiceClient().GetAsync(new GetFactorialNewTcsAsync { ForNumber = Param });
        Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
    }

    [Test]
    public async Task GetAsync_ThrowErrorAwaitAsync()
    {
        try
        {
            var response = await CreateServiceClient().GetAsync(new ThrowErrorAwaitAsync { Message = "Forbidden Test" });
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            Assert.That(ex.ErrorCode, Is.EqualTo("Forbidden"));
            Assert.That(ex.ErrorMessage, Is.EqualTo("Forbidden Test"));
        }            
    }

    [Test]
    public async Task VoidAsync()
    {
        await CreateServiceClient()
            .GetAsync(new VoidAsync { Message = "VoidAsync" });
    }

    [TestFixture]
    public class JsonAsyncTaskTests : AsyncTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new JsonServiceClient(Config.ListeningOn);
        }
    }

    [TestFixture]
    public class JsonHttpClientAsyncTaskTests : AsyncTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new JsonHttpClient(Config.ListeningOn);
        }
    }

    [TestFixture]
    public class JsvAsyncRestServiceClientTests : AsyncTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new JsvServiceClient(Config.ListeningOn);
        }
    }

    [TestFixture]
    public class XmlAsyncRestServiceClientTests : AsyncTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new XmlServiceClient(Config.ListeningOn);
        }
    }
}

[Route("/factorial/sync/{ForNumber}")]
[DataContract]
public class GetFactorialSync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/async/{ForNumber}")]
[DataContract]
public class GetFactorialGenericAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/object/{ForNumber}")]
[DataContract]
public class GetFactorialObjectAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/await/{ForNumber}")]
[DataContract]
public class GetFactorialAwaitAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/delay/{ForNumber}")]
[DataContract]
public class GetFactorialDelayAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/newtask/{ForNumber}")]
[DataContract]
public class GetFactorialNewTaskAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}

[Route("/factorial/newtcs/{ForNumber}")]
[DataContract]
public class GetFactorialNewTcsAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public long ForNumber { get; set; }
}
    
[DataContract]
public class GetFactorialResponse
{
    [DataMember]
    public long Result { get; set; }
}

[Route("/factorial/throwerror")]
[DataContract]
public class ThrowErrorAwaitAsync : IReturn<GetFactorialResponse>
{
    [DataMember]
    public string Message { get; set; }
}

[Route("/voidasync")]
[DataContract]
public class VoidAsync : IReturnVoid
{
    [DataMember]
    public string Message { get; set; }
}

public class GetFactorialAsyncService : IService
{
    public object Any(GetFactorialSync request)
    {
        return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
    }

    public Task<GetFactorialResponse> Any(GetFactorialGenericAsync request)
    {
        return Task.Factory.StartNew(() =>
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        });
    }

    public object Any(GetFactorialObjectAsync request)
    {
        return Task.Factory.StartNew(() =>
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        });
    }

    public async Task<GetFactorialResponse> Any(GetFactorialAwaitAsync request)
    {
        return await Task.Factory.StartNew(() =>
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        });
    }

    public async Task<GetFactorialResponse> Any(GetFactorialDelayAsync request)
    {
        await Task.Delay(1000);

        return await Task.Factory.StartNew(() =>
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        });
    }

    public Task<GetFactorialResponse> Any(GetFactorialNewTaskAsync request)
    {
        return new Task<GetFactorialResponse>(() =>
            new GetFactorialResponse { Result = GetFactorial(request.ForNumber) });
    }

    public Task<GetFactorialResponse> Any(GetFactorialNewTcsAsync request)
    {
        return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) }.AsTaskResult();
    }

    public async Task<GetFactorialResponse> Any(ThrowErrorAwaitAsync request)
    {
        await Task.Delay(0);
        throw new HttpError(HttpStatusCode.Forbidden, HttpStatusCode.Forbidden.ToString(), request.Message ?? "Request is forbidden");
    }

    public async Task Any(VoidAsync request)
    {
        await Task.Delay(1);
    }

    public static long GetFactorial(long n)
    {
        return n > 1 ? n * GetFactorial(n - 1) : 1;
    }
}

[Ignore("Load Test"), TestFixture]
public class AsyncLoadTests
{
    const int NoOfTimes = 1000;
     
    private ServiceStackHost appHost;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new AsyncTaskTests.AsyncTaskAppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }


    [Test]
    public void Load_test_GetFactorialSync_sync()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        for (var i = 0; i < NoOfTimes; i++)
        {
            var response = client.Get(new GetFactorialSync { ForNumber = 3 });
            if (i % 100 == 0)
            {
                "{0}: {1}".Print(i, response.Result);
            }
        }
    }

    [Test]
    public void Load_test_GetFactorialSync_HttpClient_sync()
    {
        var client = new JsonHttpClient(Config.ListeningOn);

        for (var i = 0; i < NoOfTimes; i++)
        {
            var response = client.Get(new GetFactorialSync { ForNumber = 3 });
            if (i % 100 == 0)
            {
                "{0}: {1}".Print(i, response.Result);
            }
        }
    }

    [Test]
    public async Task Load_test_GetFactorialSync_async()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        int i = 0;

        var fetchTasks = NoOfTimes.Times(() =>
            client.GetAsync(new GetFactorialSync { ForNumber = 3 })
                .ContinueWith(t =>
                {
                    if (++i % 100 == 0)
                    {
                        "{0}: {1}".Print(i, t.Result.Result);
                    }
                }));

        await Task.WhenAll(fetchTasks);
    }

    [Test]
    public async Task Load_test_GetFactorialSync_HttpClient_async()
    {
        var client = new JsonHttpClient(Config.ListeningOn);

        int i = 0;

        var fetchTasks = NoOfTimes.Times(() =>
            client.GetAsync(new GetFactorialSync { ForNumber = 3 })
                .ContinueWith(t =>
                {
                    if (++i % 100 == 0)
                    {
                        "{0}: {1}".Print(i, t.Result.Result);
                    }
                }));

        await Task.WhenAll(fetchTasks);
    }

    [Test]
    public void Load_test_GetFactorialGenericAsync_sync()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        for (var i = 0; i < NoOfTimes; i++)
        {
            var response = client.Get(new GetFactorialGenericAsync { ForNumber = 3 });
            if (i % 100 == 0)
            {
                "{0}: {1}".Print(i, response.Result);
            }
        }
    }

    [Test]
    public async Task Load_test_GetFactorialGenericAsync_async()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        int i = 0;

        var fetchTasks = NoOfTimes.Times(() =>
            client.GetAsync(new GetFactorialGenericAsync { ForNumber = 3 })
                .ContinueWith(t =>
                {
                    if (++i % 100 == 0)
                    {
                        "{0}: {1}".Print(i, t.Result.Result);
                    }
                }));

        await Task.WhenAll(fetchTasks);
    }
}