using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Examples.ServiceModel.Operations;
using ServiceStack.Examples.ServiceModel.Types;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// An example of a very basic web service
	/// </summary>
	public class ResetMovieDatabaseService : IService<ResetMovieDatabase>
	{
		public IDbConnectionFactory ConnectionFactory { get; set; }

		public object Execute(ResetMovieDatabase request)
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Movie>(true);
				dbCmd.SaveAll(ConfigureDatabase.Top5Movies);
			}

			return new ResetMovieDatabaseResponse();
		}
	}
}