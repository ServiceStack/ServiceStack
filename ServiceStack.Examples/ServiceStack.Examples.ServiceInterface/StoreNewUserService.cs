using System.Linq;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service handler that will be used to execute the request.
	/// 
	/// This example introduces the concept of a generic 'ResponseStatus' that 
	/// your service client can use to assert that the request was successful.
	/// The ResponseStatus DTO also enables you to serialize an exception in your service.
	/// 
	/// Note: This example is kept simple on purpose. In practice you would not persist your DTO's
	/// (i.e. DataContract's) directly. Instead you would use your domain models (aka ORM) for this task. 
	/// </summary>
	public class StoreNewUserService : IService<StoreNewUser>
	{
		private readonly ExampleConfig config;

		//Example of ServiceStack's IOC constructor injection
		public StoreNewUserService(ExampleConfig config)
		{
			this.config = config;
		}

		public object Execute(StoreNewUser request)
		{
			using (var dbConn = config.ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				var existingUsers = dbCmd.Select<User>("UserName = {0}", request.UserName).ToList();

				if (existingUsers.Count > 0)
				{
					return new StoreNewUserResponse {
						ResponseStatus = new ResponseStatus { ErrorCode = "UserNameMustBeUnique" }
					};
				}

				var newUser = new User { UserName = request.UserName, Email = request.Email, Password = request.Password };

				dbCmd.Insert(newUser);

				return new StoreNewUserResponse { UserId = newUser.Id };
			}
		}
	}
}