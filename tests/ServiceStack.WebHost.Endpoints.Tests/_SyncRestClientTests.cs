using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	/// <summary>
	/// These tests fail with Unauthorized exception when left last to run, 
	/// so prefixing with '_' to hoist its priority till we find out wtf is up
	/// </summary>
	public abstract class _SyncRestClientTests : IDisposable
	{
		protected const string ListeningOn = "http://localhost:85/";

		ExampleAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (appHost == null) return;			
			appHost.Dispose();
			appHost = null;
		}

		protected abstract IRestClient CreateRestClient();
		//protected virtual IRestClient CreateRestClient()
		//{
		//    return new XmlServiceClient(ListeningOn);
		//}

		[Test]
		public void Can_GET_GetFactorial_using_RestClient()
		{
			var restClient = CreateRestClient();

			var response = restClient.Get<GetFactorialResponse>("factorial/3");

			Assert.That(response, Is.Not.Null, "No response received");
			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(3)));
		}

		[Test]
		public void Can_GET_Movies_using_RestClient()
		{
			var restClient = CreateRestClient();

			var response = restClient.Get<MoviesResponse>("movies");

			Assert.That(response, Is.Not.Null, "No response received");
			Assert.That(response.Movies.EquivalentTo(ResetMoviesService.Top5Movies));
		}

		[Test]
		public void Can_GET_single_Movie_using_RestClient()
		{
			var restClient = CreateRestClient();

			var response = restClient.Get<MovieResponse>("movies/1");

			Assert.That(response, Is.Not.Null, "No response received");
			Assert.That(response.Movie.Id, Is.EqualTo(1));
		}

		[Test]
		public void Can_POST_to_add_new_Movie_using_RestClient()
		{
			var restClient = CreateRestClient();

			var newMovie = new Movie
			{
				ImdbId = "tt0450259",
				Title = "Blood Diamond",
				Rating = 8.0m,
				Director = "Edward Zwick",
				ReleaseDate = new DateTime(2007, 1, 26),
				TagLine = "A fisherman, a smuggler, and a syndicate of businessmen match wits over the possession of a priceless diamond.",
				Genres = new List<string> { "Adventure", "Drama", "Thriller" },
			};

			var response = restClient.Post<MovieResponse>("movies", newMovie);

			Assert.That(response, Is.Not.Null, "No response received");

			var createdMovie = response.Movie;
			Assert.That(createdMovie.Id, Is.GreaterThan(0));
			Assert.That(createdMovie.ImdbId, Is.EqualTo(newMovie.ImdbId));
		}

		[Test]
		public void Can_Deserialize_Xml_MovieResponse()
		{
			try
			{
				var xml = "<MovieResponse xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Movie><Director>Edward Zwick</Director><Genres xmlns:d3p1=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><d3p1:string>Adventure</d3p1:string><d3p1:string>Drama</d3p1:string><d3p1:string>Thriller</d3p1:string></Genres><Id>6</Id><ImdbId>tt0450259</ImdbId><Rating>8</Rating><ReleaseDate>2007-01-26T00:00:00+00:00</ReleaseDate><TagLine>A fisherman, a smuggler, and a syndicate of businessmen match wits over the possession of a priceless diamond.</TagLine><Title>Blood Diamond</Title></Movie></MovieResponse>";
				var response = DataContractDeserializer.Instance.Parse<MovieResponse>(xml);
				var toXml = DataContractSerializer.Instance.Parse(response);
				Console.WriteLine("XML: " + toXml);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}

		[Test]
		public void Can_DELETE_Movie_using_RestClient()
		{
			var restClient = CreateRestClient();

			var newMovie = new Movie
			{
				ImdbId = "tt0450259",
				Title = "Blood Diamond",
				Rating = 8.0m,
				Director = "Edward Zwick",
				ReleaseDate = new DateTime(2007, 1, 26),
				TagLine = "A fisherman, a smuggler, and a syndicate of businessmen match wits over the possession of a priceless diamond.",
				Genres = new List<string> { "Adventure", "Drama", "Thriller" },
			};

			var response = restClient.Post<MovieResponse>("movies", newMovie);
			var createdMovie = response.Movie;
			response = restClient.Delete<MovieResponse>("movies/" + createdMovie.Id);

			Assert.That(createdMovie, Is.Not.Null);
			Assert.That(response.Movie, Is.Null);
		}

	}

	[TestFixture]
	public class _JsonSyncRestClientTests : _SyncRestClientTests
	{
		protected override IRestClient CreateRestClient()
		{
			return new JsonServiceClient(ListeningOn);
		}

        [Test]
        public void Can_use_response_filters()
        {
            var isActioncalledGlobal = false;
            var isActioncalledLocal = false;
            ServiceClientBase.HttpWebResponseFilter = r => isActioncalledGlobal = true;
            var restClient = (JsonServiceClient)CreateRestClient();
            restClient.LocalHttpWebResponseFilter = r => isActioncalledLocal = true;
            restClient.Get<MoviesResponse>("movies");
            Assert.That(isActioncalledGlobal, Is.True);
            Assert.That(isActioncalledLocal, Is.True);
        }
	}

	[TestFixture]
	public class _JsvSyncRestClientTests : _SyncRestClientTests
	{
		protected override IRestClient CreateRestClient()
		{
			return new JsvServiceClient(ListeningOn);
		}
	}

	[TestFixture]
	public class _XmlSyncRestClientTests : _SyncRestClientTests
	{
		protected override IRestClient CreateRestClient()
		{
			return new XmlServiceClient(ListeningOn);
		}
	}
}
