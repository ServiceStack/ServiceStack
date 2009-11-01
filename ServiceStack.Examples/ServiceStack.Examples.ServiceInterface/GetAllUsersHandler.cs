using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	public class GetAllUsersHandler
		: IService<GetAllUsers>
	{
		private readonly IPersistenceProviderManager providerFactory;

		//FactoryProviderHandlerFactory in AppHost provides IOC constructor injection
		public GetAllUsersHandler(IPersistenceProviderManager providerFactory)
		{
			this.providerFactory = providerFactory;
		}

		public object Execute(GetAllUsers request)
		{
			var persistenceProvider = providerFactory.GetProvider();

			var users = persistenceProvider.GetAll<User>();

			return new GetAllUsersResponse { Users = new ArrayOfUser(users) };
		}
	}
}