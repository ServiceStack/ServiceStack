using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
    /// <summary>
    /// An example of a very basic web service
    /// </summary>
    public class RestMovieService
        : IService, IRequiresRequest
    {
        public IRequest Request { get; set; }

        public IDbConnectionFactory DbFactory { get; set; }

        public object Any(GetRestMovies request)
        {
            return Get(request.ConvertTo<RestMovies>());
        }

        public object Get(RestMovies request)
        {
            var response = new RestMoviesResponse();

            using (var db = DbFactory.Open())
            {
                if (request.Id != null)
                {
                    var movie = db.SingleById<RestMovie>(request.Id);
                    if (movie != null)
                    {
                        response.Movies.Add(movie);
                    }
                }
                else
                {
                    response.Movies = db.Select<RestMovie>();
                }
            };

            return response;
        }

        public object Put(RestMovies request)
        {
            using (var db = DbFactory.Open())
                db.Save(request.Movie);
            return new RestMoviesResponse();
        }

        public object Delete(RestMovies request)
        {
            using (var db = DbFactory.Open())
                db.DeleteById<RestMovie>(request.Id);
            return new RestMoviesResponse();
        }

        public object Post(RestMovies request)
        {
            using (var db = DbFactory.Open())
                db.Update(request.Movie);
            return new RestMoviesResponse();
        }
    }
}