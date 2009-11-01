using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service handler that will be used to execute the request.
	/// 
	/// This example shows a simple introduction into SOA-like webservices. 
	/// i.e. group similar operations into a single 'document-centric like' service request.
	/// </summary>
	public class GetUsersHandler : IService<GetUsers>
	{
		private readonly IPersistenceProviderManager providerFactory;

		//FactoryProviderHandlerFactory in AppHost provides IOC constructor injection
		public GetUsersHandler(IPersistenceProviderManager providerFactory)
		{
			this.providerFactory = providerFactory;
		}

		public object Execute(GetUsers request)
		{
			var persistenceProvider = (IQueryablePersistenceProvider)providerFactory.GetProvider();

			var users = new List<User>();

			if (request.UserIds != null)
			{
				users.AddRange(persistenceProvider.GetByIds<User>(request.UserIds));
			}

			if (request.UserNames != null)
			{
				users.AddRange(persistenceProvider.FindByValues<User>("UserName", request.UserNames));
			}

			return new GetUsersResponse { Users = new ArrayOfUser(users) };
		}
	}
}