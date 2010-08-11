using ServiceStack.Examples.ServiceModel.Operations;
using ServiceStack.Examples.ServiceModel.Types;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// An example of a basic REST web service
	/// 
	/// Each operation needs to support same Request and Response DTO's so you will
	/// need to combine the types of all your operations into the same DTO as done
	/// in this example.
	/// </summary>
	public class MovieRestService
		: IService<Movies>
		, IRestGetService<Movies>
		, IRestPutService<Movies>
		, IRestPostService<Movies>
		, IRestDeleteService<Movies>
		, IRequiresRequestContext //Ask ServiceStack to inject the RequestContext
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MovieRestService));

		public IRequestContext RequestContext { get; set; }
		
		public IDbConnectionFactory ConnectionFactory { get; set; }

		public object Execute(Movies request)
		{
			return Get(request);
		}

		/// <summary>
		/// GET /Movies 
		/// GET /Movies?Id={Id}
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object Get(Movies request)
		{
			//Alternatively you can infer the HTTP method by inspecting the RequestContext attributes
			Log.InfoFormat("Using RequestContext to inspect Endpoint attributes: {0}",
				this.RequestContext.EndpointAttributes);

			var response = new MoviesResponse();

			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				if (request.Id != null)
				{
					// GET /Movies?Id={request.Id}
					var movie = dbCmd.GetByIdOrDefault<Movie>(request.Id);
					if (movie != null)
					{
						response.Movies.Add(movie);
					}
				}
				else
				{
					// GET /Movies
					response.Movies = dbCmd.Select<Movie>();
				}
			}

			return response;
		}

		/// <summary>
		/// PUT /Movies
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object Put(Movies request)
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.Insert(request.Movie);
			}

			return new MoviesResponse();
		}

		/// <summary>
		/// DELETE /Movies
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object Delete(Movies request)
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.DeleteById<Movie>(request.Id);
			}

			return new MoviesResponse();
		}

		/// <summary>
		/// POST /Movies
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object Post(Movies request)
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.Update(request.Movie);
			}

			return new MoviesResponse();
		}
	}

}