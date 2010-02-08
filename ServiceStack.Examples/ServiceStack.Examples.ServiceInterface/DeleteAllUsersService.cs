using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// </summary>
	public class DeleteAllUsersService 
		: IService<DeleteAllUsers>
	{
		//Example of ServiceStack's IOC property injection
		public ExampleConfig Config { get; set; }

		public object Execute(DeleteAllUsers request)
		{
			using (var dbConn = Config.ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.DeleteAll<User>();

				return new DeleteAllUsersResponse();
			}
		}
	}

}