using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Data;
using ServiceStack.MiniProfiler;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[RestService("/profiler", "GET")]
	[RestService("/profiler/{Type}", "GET")]
	public class MiniProfiler
	{
		public string Type { get; set; }
	}

	public class MiniProfilerService : ServiceBase<MiniProfiler>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		protected override object Run(MiniProfiler request)
		{
			var profiler = Profiler.Current;

			using (var dbConn = DbFactory.OpenDbConnection())
			using (profiler.Step("MiniProfiler Service"))
			{
				if (request.Type.IsNullOrEmpty())
				{
					using (profiler.Step("Simple Select all"))
					{
						return DbFactory.Exec(dbCmd => dbCmd.Select<Movie>());
					}
				}
				if (request.Type == "n1")
				{
					using (profiler.Step("N + 1 query"))
					{
						var results = new List<Movie>();
						var dbCmd = dbConn.CreateCommand();
						foreach (var movie in dbCmd.Select<Movie>())
						{
							results.Add(dbCmd.QueryById<Movie>(movie.Id));
						}
						return results;
					}
				}
			}

			return null;
		}
	}
}