using System;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;

namespace ServiceStack.Examples.ServiceInterface.Support
{
	public class ConfigureDatabase
	{
		public static void Init(IDbConnectionFactory connectionFactory)
		{
			try
			{
				using (var dbConn = connectionFactory.OpenDbConnection())
				using (var dbCmd = dbConn.CreateCommand())
				{
					dbCmd.CreateTable<User>(false);
				}

			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}

}