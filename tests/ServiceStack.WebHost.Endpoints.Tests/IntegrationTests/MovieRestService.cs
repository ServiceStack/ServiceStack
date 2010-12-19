using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	/// <summary>
	/// An example of a very basic web service
	/// </summary>
	public class MovieRestService
		: IService<Movies>
		  , IRestGetService<Movies>
		  , IRestPutService<Movies>
		  , IRestPostService<Movies>
		  , IRestDeleteService<Movies>
		  , IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		public IDbConnectionFactory DbFactory { get; set; }

		public object Execute(Movies request)
		{
			return Get(request);
		}

		public object Get(Movies request)
		{
			var response = new MoviesResponse();

			DbFactory.Exec(dbCmd =>
			{
				if (request.Id != null)
				{
					var movie = dbCmd.GetByIdOrDefault<Movie>(request.Id);
					if (movie != null)
					{
						response.Movies.Add(movie);
					}
				}
				else
				{
					response.Movies = dbCmd.Select<Movie>();
				}
			});

			return response;
		}

		public object Put(Movies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.Save(request.Movie));
			return new MoviesResponse();
		}

		public object Delete(Movies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.DeleteById<Movie>(request.Id));
			return new MoviesResponse();
		}

		public object Post(Movies request)
		{
			DbFactory.Exec(dbCmd => dbCmd.Update(request.Movie));
			return new MoviesResponse();
		}
	}
}