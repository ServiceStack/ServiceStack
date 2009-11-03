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
	public class GetUsersService 
		: IService<GetUsers>
	{
		public IPersistenceProviderManager ProviderManager { get; set; }

		public object Execute(GetUsers request)
		{
			var persistenceProvider = (IQueryablePersistenceProvider)ProviderManager.GetProvider();

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