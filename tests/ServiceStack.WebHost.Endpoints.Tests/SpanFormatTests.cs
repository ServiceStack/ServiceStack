using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CreateMovies : IReturn<CreateMovies>
    {
        public List<Tests.Support.Host.Movie> Movies { get; set; }
    }

    public class CreateMoviesAsync : IReturn<CreateMoviesAsync>
    {
        public List<Tests.Support.Host.Movie> Movies { get; set; }
    }

    public class CreateMoviesService : Service
    {
        public object Any(CreateMovies request) => request;

        public async Task<object> Any(CreateMoviesAsync request)
        {
            await Task.Yield();
            return request;
        }
    }
    
    public class SpanFormatTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(SpanFormatTests), typeof(SpanFormatTests).Assembly) {}

            public override void Configure(Container container)
            {
            }
        }

        private readonly ServiceStackHost appHost;

        public SpanFormatTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            appHost.Dispose();
            DefaultMemory.Configure();
        }

        [Test]
        public void Does_deserialize_json_RequestBody()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Post(new CreateMovies {
                Movies = ResetMoviesService.Top5Movies
            });

            Assert.That(response.Movies, Is.EquivalentTo(ResetMoviesService.Top5Movies));

            var responseAsync = client.Post(new CreateMoviesAsync {
                Movies = ResetMoviesService.Top5Movies
            });

            Assert.That(responseAsync.Movies, Is.EquivalentTo(ResetMoviesService.Top5Movies));
        }

        [Test]
        public async Task Does_deserialize_json_RequestBody_Async()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = await client.PostAsync(new CreateMovies {
                Movies = ResetMoviesService.Top5Movies
            });

            Assert.That(response.Movies, Is.EquivalentTo(ResetMoviesService.Top5Movies));

            var responseAsync = await client.PostAsync(new CreateMoviesAsync {
                Movies = ResetMoviesService.Top5Movies
            });

            Assert.That(responseAsync.Movies, Is.EquivalentTo(ResetMoviesService.Top5Movies));
        }
    }
}