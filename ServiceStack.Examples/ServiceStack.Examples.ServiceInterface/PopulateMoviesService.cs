using System;
using System.Collections.Generic;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// An example of a very basic web service
	/// </summary>
	public class PopulateMoviesService : IService<PopulateMovies>
	{
		public IDbConnectionFactory ConnectionFactory { get; set; }

		public object Execute(PopulateMovies request)
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.SaveAll(ConfigureDatabase.Top5Movies);
			}

			return new PopulateMoviesResponse();
		}
	}
}