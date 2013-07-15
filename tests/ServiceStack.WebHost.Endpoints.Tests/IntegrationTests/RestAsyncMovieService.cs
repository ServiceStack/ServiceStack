using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class RestAsyncMovies : RestMovies
	{}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class RestAsyncMoviesResponse : RestMoviesResponse
	{}

	/// <summary>
	/// An example of a async web service
	/// </summary>
	public class RestAsyncMovieService
		: IService<RestAsyncMovies>
			, IRestGetService<RestAsyncMovies>
			, IRestPutService<RestAsyncMovies>
			, IRestPostService<RestAsyncMovies>
			, IRestDeleteService<RestAsyncMovies>
		  , IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		public IDbConnectionFactory DbFactory { get; set; }

		public object Execute(RestAsyncMovies request)
		{
			return Get(request);
		}

		public object Get(RestAsyncMovies request)
		{
			return Task.Factory.StartNew<object>(() =>
			{
				var response = new RestAsyncMoviesResponse();

				DbFactory.Run(db =>
				{
					if (request.Id != null)
					{
						var movie = db.GetByIdOrDefault<RestMovie>(request.Id);
						if (movie != null)
						{
							response.Movies.Add(movie);
						}
					}
					else
					{
						response.Movies = db.Select<RestMovie>();
					}
				});

				return response;
			});
		}

		public object Put(RestAsyncMovies request)
		{
			return Task.Factory.StartNew<object>(() =>
			{
				DbFactory.Run(db => db.Save(request.Movie));
				return new RestAsyncMoviesResponse();
			});
		}

		public object Delete(RestAsyncMovies request)
		{
			return Task.Factory.StartNew<object>(() =>
			{
				DbFactory.Run(db => db.DeleteById<RestMovie>(request.Id));
				return new RestAsyncMoviesResponse();
			});
		}

		public object Post(RestAsyncMovies request)
		{
			return Task.Factory.StartNew<object>(() =>
			{
				DbFactory.Run(db => db.Update(request.Movie));
				return new RestAsyncMoviesResponse();
			});
		}
	}
}