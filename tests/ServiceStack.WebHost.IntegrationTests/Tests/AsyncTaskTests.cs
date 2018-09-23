using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public abstract class AsyncTaskTests
    {
        private const string ListeningOn = Config.ServiceStackBaseUri;

        protected abstract IServiceClient CreateServiceClient();

        private const int Param = 3;

        [Test]
        public void GetSync_GetFactorialGenericSync()
        {
            var response = CreateServiceClient().Get(new GetFactorialSync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialGenericAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialGenericAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialObjectAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialObjectAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialAwaitAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialAwaitAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialDelayAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialDelayAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialUnmarkedAsync()
        {
            var response = CreateServiceClient().Get<GetFactorialResponse>(
                new GetFactorialUnmarkedAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
        }


        [Test]
        public async Task GetAsync_GetFactorialGenericSync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialSync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialGenericAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialGenericAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialObjectAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialObjectAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialAwaitAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialAwaitAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialDelayAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialDelayAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
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
        public async Task GetAsync_GetFactorialUnmarkedAsync()
        {
            var response = await CreateServiceClient().GetAsync<GetFactorialResponse>(
                new GetFactorialUnmarkedAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
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
                return new JsonServiceClient(ListeningOn);
            }
        }

        [TestFixture]
        public class JsvAsyncRestServiceClientTests : AsyncTaskTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsvServiceClient(ListeningOn);
            }
        }

        [TestFixture]
        public class XmlAsyncRestServiceClientTests : AsyncTaskTests
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new XmlServiceClient(ListeningOn);
            }
        }
    }

    [TestFixture]
    public class Soap12AsyncPostTaskTests : AsyncPostTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new Soap12ServiceClient(Config.ServiceStackBaseUri);
        }
    }

    [TestFixture]
    public class JsonServiceClientAsyncPostTaskTests : AsyncPostTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new JsonServiceClient(Config.ServiceStackBaseUri);
        }
    }

    [TestFixture]
    public class JsonHttpClientAsyncPostTaskTests : AsyncPostTaskTests
    {
        protected override IServiceClient CreateServiceClient()
        {
            return new JsonHttpClient(Config.ServiceStackBaseUri);
        }
    }

    public abstract class AsyncPostTaskTests
    {
        private const int Param = 3;

        protected abstract IServiceClient CreateServiceClient();

        [Test]
        public void PostSync_GetFactorialGenericSync()
        {
            var response = CreateServiceClient().Post(new GetFactorialSync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void PostSync_GetFactorialGenericAsync()
        {
            var response = CreateServiceClient().Post(new GetFactorialGenericAsync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void PostSync_GetFactorialObjectAsync()
        {
            var response = CreateServiceClient().Post(new GetFactorialObjectAsync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void PostSync_GetFactorialAwaitAsync()
        {
            var response = CreateServiceClient().Post(new GetFactorialAwaitAsync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void PostSync_GetFactorialDelayAsync()
        {
            var response = CreateServiceClient().Post(new GetFactorialDelayAsync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(Param)));
        }

        [Test]
        public void PostSync_GetFactorialUnmarkedAsync()
        {
            var response = CreateServiceClient().Post<GetFactorialResponse>(
                new GetFactorialUnmarkedAsync {ForNumber = Param});
            Assert.That(response.Result, Is.EqualTo(GetFactorialAsyncService.GetFactorial(Param)));
        }
    }

    [Ignore("Load Test"), TestFixture]
    public class AsyncLoadTests
    {
        const int NoOfTimes = 1000;

        [Test]
        public void Load_test_GetFactorialSync_sync()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

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
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

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
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

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
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

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
}