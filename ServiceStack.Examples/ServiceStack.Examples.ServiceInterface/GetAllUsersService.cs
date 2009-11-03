using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	public class GetAllUsersService
		: IService<GetAllUsers>
	{
		public IPersistenceProviderManager ProviderManager { get; set; }

		public object Execute(GetAllUsers request)
		{
			var persistenceProvider = ProviderManager.GetProvider();

			var users = persistenceProvider.GetAll<User>();

			return new GetAllUsersResponse { Users = new ArrayOfUser(users) };
		}
	}
}