using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using Funq;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Plugins.ProtoBuf;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.IntegrationTests;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{

	[Route("/factorial/{ForNumber}")]
	[DataContract]
	public class GetFactorial
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

	public class GetFactorialService : IService
	{
		public object Any(GetFactorial request)
		{
			return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
		}

		public static long GetFactorial(long n)
		{
			return n > 1 ? n * GetFactorial(n - 1) : 1;
		}
	}

	[DataContract]
	public class AlwaysThrows { }

	[DataContract]
	public class AlwaysThrowsResponse : IHasResponseStatus
	{
		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AlwaysThrowsService : ServiceInterface.Service
	{
	    public object Any(AlwaysThrows request)
		{
			throw new ArgumentException("This service always throws an error");
		}
	}


	[Route("/movies", "POST,PUT")]
	[Route("/movies/{Id}")]
	[DataContract]
	public class Movie
	{
		public Movie()
		{
			this.Genres = new List<string>();
		}

        [DataMember(Order = 1)]
		[AutoIncrement]
		public int Id { get; set; }

        [DataMember(Order = 2)]
		public string ImdbId { get; set; }

        [DataMember(Order = 3)]
		public string Title { get; set; }

        [DataMember(Order = 4)]
		public decimal Rating { get; set; }

        [DataMember(Order = 5)]
		public string Director { get; set; }

        [DataMember(Order = 6)]
		public DateTime ReleaseDate { get; set; }

        [DataMember(Order = 7)]
		public string TagLine { get; set; }

        [DataMember(Order = 8)]
		public List<string> Genres { get; set; }

		#region AutoGen ReSharper code, only required by tests
		public bool Equals(Movie other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.ImdbId, ImdbId)
				&& Equals(other.Title, Title)
				&& other.Rating == Rating
				&& Equals(other.Director, Director)
				&& other.ReleaseDate.Equals(ReleaseDate)
				&& Equals(other.TagLine, TagLine)
				&& Genres.EquivalentTo(other.Genres);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(Movie)) return false;
			return Equals((Movie)obj);
		}

		public override int GetHashCode()
		{
			return ImdbId != null ? ImdbId.GetHashCode() : 0;
		}
		#endregion
	}

	[DataContract]
	public class MovieResponse
	{
		[DataMember]
		public Movie Movie { get; set; }
	}


    public class MovieService : ServiceInterface.Service
	{
		public IDbConnectionFactory DbFactory { get; set; }

		/// <summary>
		/// GET /movies/{Id} 
		/// </summary>
		public object Get(Movie movie)
		{
			return new MovieResponse {
				Movie = DbFactory.Run(db => db.GetById<Movie>(movie.Id))
			};
		}

		/// <summary>
		/// POST /movies
		/// </summary>
		public object Post(Movie movie)
		{
			var newMovieId = DbFactory.Run(db => {
				db.Insert(movie);
				return db.GetLastInsertId();
			});

			var newMovie = new MovieResponse {
				Movie = DbFactory.Run(db => db.GetById<Movie>(newMovieId))
			};
			return new HttpResult(newMovie) {
				StatusCode = HttpStatusCode.Created,
				Headers = {
					{ HttpHeaders.Location, this.RequestContext.AbsoluteUri.WithTrailingSlash() + newMovieId }
				}
			};
		}

		/// <summary>
		/// PUT /movies
		/// </summary>
		public object Put(Movie movie)
		{
			DbFactory.Run(db => db.Save(movie));
			return new MovieResponse();
		}

		/// <summary>
		/// DELETE /movies/{Id}
		/// </summary>
		public object Delete(Movie request)
		{
			DbFactory.Run(db => db.DeleteById<Movie>(request.Id));
			return new MovieResponse();
		}
	}


	[DataContract]
	[Route("/movies", "GET")]
    [Route("/movies/genres/{Genre}")]
	public class Movies
	{
		[DataMember]
		public string Genre { get; set; }

		[DataMember]
		public Movie Movie { get; set; }
	}

	[DataContract]
	public class MoviesResponse
	{
		public MoviesResponse()
		{
			Movies = new List<Movie>();
		}

		[DataMember(Order = 1)]
		public List<Movie> Movies { get; set; }
	}

    public class MoviesService : ServiceInterface.Service
	{
		/// <summary>
		/// GET /movies 
		/// GET /movies/genres/{Genre}
		/// </summary>
		public object Get(Movies request)
		{
			var response = new MoviesResponse {
				Movies = request.Genre.IsNullOrEmpty()
					? Db.Select<Movie>()
					: Db.Select<Movie>("Genres LIKE {0}", "%" + request.Genre + "%")
			};

			return response;
		}
	}

	public class MoviesZip
	{
		public string Genre { get; set; }
		public Movie Movie { get; set; }
	}

	public class MoviesZipResponse
	{
		public MoviesZipResponse()
		{
			Movies = new List<Movie>();
		}

		public List<Movie> Movies { get; set; }
	}

    public class MoviesZipService : ServiceInterface.Service
	{
		public IDbConnectionFactory DbFactory { get; set; }

		public object Get(MoviesZip request)
		{
			return Post(request);
		}

		public object Post(MoviesZip request)
		{
			var response = new MoviesZipResponse {
				Movies = request.Genre.IsNullOrEmpty()
					? DbFactory.Run(db => db.Select<Movie>())
					: DbFactory.Run(db => db.Select<Movie>("Genres LIKE {0}", "%" + request.Genre + "%"))
			};

			return RequestContext.ToOptimizedResult(response);
		}
	}


	[DataContract]
	[Route("/reset-movies")]
	public class ResetMovies { }

	[DataContract]
	public class ResetMoviesResponse
		: IHasResponseStatus
	{
		public ResetMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ResetMoviesService : ServiceInterface.Service
	{
		public static List<Movie> Top5Movies = new List<Movie>
		{
			new Movie { ImdbId = "tt0111161", Title = "The Shawshank Redemption", Rating = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, },
			new Movie { ImdbId = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, },
			new Movie { ImdbId = "tt1375666", Title = "Inception", Rating = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, },
			new Movie { ImdbId = "tt0071562", Title = "The Godfather: Part II", Rating = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, },
			new Movie { ImdbId = "tt0060196", Title = "The Good, the Bad and the Ugly", Rating = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, },
		};

		public object Post(ResetMovies request)
		{
            const bool overwriteTable = true;
            Db.CreateTable<Movie>(overwriteTable);
            Db.SaveAll(Top5Movies);

			return new ResetMoviesResponse();
		}
	}

	[DataContract]
	public class GetHttpResult { }

	[DataContract]
	public class GetHttpResultResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class HttpResultService : IService
	{
		public object Any(GetHttpResult request)
		{
			var getHttpResultResponse = new GetHttpResultResponse { Result = "result" };
			return new HttpResult(getHttpResultResponse);
		}
	}


    [Route("/inbox/{Id}/responses", "GET, PUT, OPTIONS")]
    public class InboxPostResponseRequest
    {
        public int Id { get; set; }
        public List<PageElementResponseDTO> Responses { get; set; }
    }

    public class InboxPostResponseRequestResponse
    {
        public int Id { get; set; }
        public List<PageElementResponseDTO> Responses { get; set; }
    }

    public class PageElementResponseDTO
    {
        public int PageElementId { get; set; }
        public string PageElementType { get; set; }
        public string PageElementResponse { get; set; }
    }

    public class InboxPostResponseRequestService : ServiceInterface.Service
    {
        public object Any(InboxPostResponseRequest request)
        {
            if (request.Responses == null || request.Responses.Count == 0)
            {
                throw new ArgumentNullException("Responses");
            }
            return new InboxPostResponseRequestResponse {
                Id = request.Id,
                Responses = request.Responses
            };
        }
    }

    [Route("/inbox/{Id}/responses", "GET, PUT, OPTIONS")]
    public class InboxPost
    {
        public bool Throw { get; set; }
        public int Id { get; set; }
    }

    public class InboxPostService : ServiceInterface.Service
    {
        public object Any(InboxPost request)
        {
            if (request.Throw)
                throw new ArgumentNullException("Throw");
            
            return null;
        }
    }

    [DataContract]
    [Route("/long_running")]
    public class LongRunning { }

    public class LongRunningService : ServiceInterface.Service
    {
        public object Any(LongRunning request)
        {
            Thread.Sleep(5000);

            return "LongRunning done.";
        }
    }

    public class ExampleAppHostHttpListener
		: AppHostHttpListenerBase
	{
		//private static ILog log;

		public ExampleAppHostHttpListener()
			: base("ServiceStack Examples", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			//log = LogManager.GetLogger(typeof(ExampleAppHostHttpListener));
		}

		public Action<Container> ConfigureFilter { get; set; }

		public override void Configure(Container container)
		{
			EndpointHostConfig.Instance.GlobalResponseHeaders.Clear();

			//Signal advanced web browsers what HTTP Methods you accept
			base.SetConfig(new EndpointHostConfig {
				GlobalResponseHeaders =
				{
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
				},
				WsdlServiceNamespace = "http://www.servicestack.net/types",
				LogFactory = new ConsoleLogFactory(),
				DebugMode = true,
			});

			this.RegisterRequestBinder<CustomRequestBinder>(
				httpReq => new CustomRequestBinder { IsFromBinder = true });

			Routes
				.Add<Movies>("/custom-movies", "GET")
				.Add<Movies>("/custom-movies/genres/{Genre}")
				.Add<Movie>("/custom-movies", "POST,PUT")
				.Add<Movie>("/custom-movies/{Id}")
				.Add<GetFactorial>("/fact/{ForNumber}")
				.Add<MoviesZip>("/movies.zip")
				.Add<GetHttpResult>("/gethttpresult")
			;

			container.Register<IResourceManager>(new ConfigurationResourceManager());

			//var appSettings = container.Resolve<IResourceManager>();

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			//var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					":memory:", false,
					SqliteOrmLiteDialectProvider.Instance));

			var resetMovies = container.Resolve<ResetMoviesService>();
			resetMovies.Post(null);

			//var movies = container.Resolve<IDbConnectionFactory>().Exec(x => x.Select<Movie>());
			//Console.WriteLine(movies.Dump());

			if (ConfigureFilter != null)
			{
				ConfigureFilter(container);
			}

            Plugins.Add(new ProtoBufFormat());
            Plugins.Add(new RequestInfoFeature());
		}
	}

    public class ExampleAppHostHttpListenerLongRunning
    : AppHostHttpListenerLongRunningBase
    {
        //private static ILog log;

        public ExampleAppHostHttpListenerLongRunning()
            : base("ServiceStack Examples", 500, typeof(GetFactorialService).Assembly)
        {
            LogManager.LogFactory = new DebugLogFactory();
            //log = LogManager.GetLogger(typeof(ExampleAppHostHttpListener));
        }

        public Action<Container> ConfigureFilter { get; set; }

        public override void Configure(Container container)
        {
            EndpointHostConfig.Instance.GlobalResponseHeaders.Clear();

            //Signal advanced web browsers what HTTP Methods you accept
            base.SetConfig(new EndpointHostConfig
            {
                GlobalResponseHeaders =
				{
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
				},
                WsdlServiceNamespace = "http://www.servicestack.net/types",
                LogFactory = new ConsoleLogFactory(),
                DebugMode = true,
            });

            this.RegisterRequestBinder<CustomRequestBinder>(
                httpReq => new CustomRequestBinder { IsFromBinder = true });

            Routes
                .Add<Movies>("/custom-movies", "GET")
                .Add<Movies>("/custom-movies/genres/{Genre}")
                .Add<Movie>("/custom-movies", "POST,PUT")
                .Add<Movie>("/custom-movies/{Id}")
                .Add<GetFactorial>("/fact/{ForNumber}")
                .Add<MoviesZip>("/movies.zip")
                .Add<GetHttpResult>("/gethttpresult")
            ;

            container.Register<IResourceManager>(new ConfigurationResourceManager());

            //var appSettings = container.Resolve<IResourceManager>();

            container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
            //var appConfig = container.Resolve<ExampleConfig>();

            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(
                    ":memory:", false,
                    SqliteOrmLiteDialectProvider.Instance));

            var resetMovies = container.Resolve<ResetMoviesService>();
            resetMovies.Post(null);

            //var movies = container.Resolve<IDbConnectionFactory>().Exec(x => x.Select<Movie>());
            //Console.WriteLine(movies.Dump());

            if (ConfigureFilter != null)
            {
                ConfigureFilter(container);
            }

            Plugins.Add(new RequestInfoFeature());
        }
    }

}