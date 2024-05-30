using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests;

public abstract class AsyncRestClientTests
{
    private const string ListeningOn = Config.BaseUriHost;

    ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new ExampleAppHostHttpListener()
            .Init()
            .Start(ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    protected abstract IHttpRestClientAsync CreateAsyncRestClient();

    [Test]
    public async Task Can_call_GetAsync_on_GetFactorial_using_RestClientAsync()
    {
        var asyncClient = CreateAsyncRestClient();

        var response = await asyncClient.GetAsync<GetFactorialResponse>("factorial/3");

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(3)));
    }

    [Test]
    public async Task Can_call_GetAsync_on_Movies_using_RestClientAsync()
    {
        var asyncClient = CreateAsyncRestClient();

        var response = await asyncClient.GetAsync<MoviesResponse>("all-movies");

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(response.Movies.EquivalentTo(ResetMoviesService.Top5Movies));
    }

    [Test]
    public async Task Can_call_GetAsync_on_single_Movie_using_RestClientAsync()
    {
        var asyncClient = CreateAsyncRestClient();

        var response = await asyncClient.GetAsync<MovieResponse>("all-movies/1");

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(response.Movie.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_call_PostAsync_to_add_new_Movie_using_RestClientAsync()
    {
        var asyncClient = CreateAsyncRestClient();

        var newMovie = new Support.Host.Movie
        {
            ImdbId = "tt0450259",
            Title = "Blood Diamond",
            Rating = 8.0m,
            Director = "Edward Zwick",
            ReleaseDate = new DateTime(2007, 1, 26),
            TagLine = "A fisherman, a smuggler, and a syndicate of businessmen match wits over the possession of a priceless diamond.",
            Genres = ["Adventure", "Drama", "Thriller"],
        };

        var response = await asyncClient.PostAsync<MovieResponse>("all-movies", newMovie);

        Assert.That(response, Is.Not.Null, "No response received");

        var createdMovie = response.Movie;
        Assert.That(createdMovie.Id, Is.GreaterThan(0));
        Assert.That(createdMovie.ImdbId, Is.EqualTo(newMovie.ImdbId));
    }

    [Test]
    public async Task Can_call_DeleteAsync_to_delete_Movie_using_RestClientAsync()
    {
        var asyncClient = CreateAsyncRestClient();

        var newMovie = new Support.Host.Movie
        {
            ImdbId = "tt0450259",
            Title = "Blood Diamond",
            Rating = 8.0m,
            Director = "Edward Zwick",
            ReleaseDate = new DateTime(2007, 1, 26),
            TagLine = "A fisherman, a smuggler, and a syndicate of businessmen match wits over the possession of a priceless diamond.",
            Genres = new List<string> { "Adventure", "Drama", "Thriller" },
        };

        var response = await asyncClient.PostAsync<MovieResponse>("all-movies", newMovie);

        var createdMovie = response.Movie;

        response = await asyncClient.DeleteAsync<MovieResponse>("all-movies/" + createdMovie.Id);

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(createdMovie, Is.Not.Null);
        Assert.That(response.Movie, Is.Null);
    }

    [TestFixture]
    public class JsonAsyncRestServiceClientTests : AsyncRestClientTests
    {
        protected override IHttpRestClientAsync CreateAsyncRestClient()
        {
            return new JsonServiceClient(ListeningOn);
        }
    }

    [TestFixture]
    public class JsonAsyncRestServiceHttpClientTests : AsyncRestClientTests
    {
        protected override IHttpRestClientAsync CreateAsyncRestClient()
        {
            return new JsonHttpClient(ListeningOn);
        }
    }

    [TestFixture]
    public class JsvAsyncRestServiceClientTests : AsyncRestClientTests
    {
        protected override IHttpRestClientAsync CreateAsyncRestClient()
        {
            return new JsvServiceClient(ListeningOn);
        }
    }

    [TestFixture]
    public class XmlAsyncRestServiceClientTests : AsyncRestClientTests
    {
        protected override IHttpRestClientAsync CreateAsyncRestClient()
        {
            return new XmlServiceClient(ListeningOn);
        }
    }
}