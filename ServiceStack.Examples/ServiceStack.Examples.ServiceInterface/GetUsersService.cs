using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service handler that will be used to execute the request.
	/// 
	/// This example shows a simple introduction into SOA-like webservices. 
	/// i.e. group similar operations into a single 'document-centric like' service request.
	/// </summary>
	public class GetUsersService 
		: IService<GetUsers>
	{
		//Example of ServiceStack's IOC property injection
		public ExampleConfig Config { get; set; }

		public object Execute(GetUsers request)
		{
			using (var dbConn = Config.ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				var users = new List<User>();

				if (request.UserIds != null)
				{
					users.AddRange(dbCmd.GetByIds<User>(request.UserIds));
				}

				if (request.UserNames != null)
				{
					users.AddRange(dbCmd.Select<User>("UserName IN ({0})", 
						request.UserNames.SqlJoin()));
				}

				return new GetUsersResponse { Users = new ArrayOfUser(users) };
			}
		}
	}
}