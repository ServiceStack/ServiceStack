using System.Collections.Generic;
using System.Linq;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Exclude(Feature.Soap)]
	[Route("/profiler", "GET")]
	[Route("/profiler/{Type}", "GET")]
	public class MiniProfiler
	{
		public string Type { get; set; }
	}

	public class MiniProfilerService : Service
	{
		public IDbConnectionFactory DbFactory { get; set; }

        public object Any(MiniProfiler request)
		{
			var profiler = Profiler.Current;

			using (var db = DbFactory.OpenDbConnection())
			using (profiler.Step("MiniProfiler Service")) 
			{
				if (request.Type == "n1")
				{
					using (profiler.Step("N + 1 query"))
					{
					    return db.Select<Movie>().Select(movie => db.SingleById<Movie>(movie.Id)).ToList();
					}
				}

				using (profiler.Step("Simple Select all"))
				{
					return db.Select<Movie>();
				}
			}
		}
	}
}