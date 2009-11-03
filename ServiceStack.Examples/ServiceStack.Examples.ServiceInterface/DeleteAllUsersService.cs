using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
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
		public IPersistenceProviderManager ProviderManager { get; set; }

		public object Execute(DeleteAllUsers request)
		{
			//Get the persistence provider registered in the AppHost
			var persistenceProvider = ProviderManager.GetProvider();

			var deleteUsers = persistenceProvider.GetAll<User>();

			persistenceProvider.DeleteAll(deleteUsers);

			return new DeleteAllUsersResponse();
		}
	}
}