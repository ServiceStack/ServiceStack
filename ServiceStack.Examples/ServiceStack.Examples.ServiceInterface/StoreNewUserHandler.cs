using System.Linq;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
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
	/// Note: This example is kept simple on purpose. In practice you would never persist your Data Transfer Objects
	/// (i.e. DataContract's) directly. Instead you should be using other persisted domain models (aka ORM) for this purpose. 
	/// </summary>
	public class StoreNewUserHandler : IService<StoreNewUser>
	{
		private readonly IPersistenceProviderManager providerFactory;

		//FactoryProviderHandlerFactory in AppHost provides IOC constructor injection
		public StoreNewUserHandler(IPersistenceProviderManager providerFactory)
		{
			this.providerFactory = providerFactory;
		}

		public object Execute(StoreNewUser request)
		{
			var persistenceProvider = (IQueryablePersistenceProvider)providerFactory.GetProvider();

			var existingUsers = persistenceProvider.Query<User>(user => user.UserName == request.UserName).ToList();

			if (existingUsers.Count > 0)
			{
				return new StoreNewUserResponse {
					ResponseStatus = new ResponseStatus { ErrorCode = "UserNameMustBeUnique" }
				};
			}

			var newUser = new User { UserName = request.UserName, Email = request.Email, Password = request.Password };

			persistenceProvider.Store(newUser);

			return new StoreNewUserResponse { UserId = newUser.Id };
		}
	}
}