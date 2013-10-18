using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public abstract class AsyncTaskTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AsyncTaskAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract IServiceClient CreateServiceClient();

        private const int Param = 3;

        public class AsyncTaskAppHost : AppHostHttpListenerBase
        {
            public AsyncTaskAppHost() 
                : base(typeof(AsyncTaskAppHost).Name, typeof(AsyncTaskAppHost).Assembly) {}

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


        [TestFixture]
        public class JsonAsyncTaskTests : AsyncTaskTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsonServiceClient(Config.ListeningOn);
            }
        }

        //[TestFixture]
        //public class JsvAsyncRestServiceClientTests : AsyncTaskTests
        //{
        //    protected override IServiceClient CreateServiceClient()
        //    {
        //        return new JsvServiceClient(ListeningOn);
        //    }
        //}

        //[TestFixture]
        //public class XmlAsyncRestServiceClientTests : AsyncTaskTests
        //{
        //    protected override IServiceClient CreateServiceClient()
        //    {
        //        return new XmlServiceClient(ListeningOn);
        //    }
        //}
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

        public static long GetFactorial(long n)
        {
            return n > 1 ? n * GetFactorial(n - 1) : 1;
        }
    }

    [TestFixture]
    public class TaskTests
    {
        [Test]
        public void test()
        {
            var result = "exec";
            var task = new Task(() => result.Print());
            task.Status.ToString().Print();

            var tcs = new TaskCompletionSource<string>();
            tcs.Task.Status.ToString().Print();
            tcs.SetResult(result);            
            tcs.Task.Status.ToString().Print();            

            task.RunSynchronously();
        }
    }
}