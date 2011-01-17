using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	/// <summary>
	/// An example of a very basic web service
	/// </summary>
	public class RestMovieService
		: IService<RestMovies>
		  , IRestGetService<RestMovies>
		  , IRestPutService<RestMovies>
		  , IRestPostService<RestMovies>
		  , IRestDeleteService<RestMovies>
		  , IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		public IDbConnectionFactory DbFactory { get; set; }

		public object Execute(RestMovies request)
		{
			return Get(request);
		}

		public object Get(RestMovies request)
		{
			var response = new RestMoviesResponse();

			DbFactory.Exec(dbCmd =>
			{
				if (request.Id != null)
				{
					var movie = dbCmd.GetByIdOrDefault<RestMovie>(request.Id);
					if (movie != null)
					{
						response.Movies.Add(movie);
					}
				}
				else
				{
					response.Movies = dbCmd.Select<RestMovie>();
				}
			});

			return response;
		}

		public object Put(RestMovies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.Save(request.Movie));
			return new RestMoviesResponse();
		}

		public object Delete(RestMovies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.DeleteById<RestMovie>(request.Id));
			return new RestMoviesResponse();
		}

		public object Post(RestMovies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.Update(request.Movie));
			return new RestMoviesResponse();
		}
	}
}