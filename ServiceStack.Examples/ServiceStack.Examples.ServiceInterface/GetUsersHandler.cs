using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// 
	/// This example shows a simple introduction into SOA-like webservices. 
	/// i.e. group similar operations into a single 'document-centric like' service request.
	/// </summary>
	[Port(typeof(GetUsers))]
	public class GetUsersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetUsers>();

			//Get the persistence provider registered in the AppHost
			var persistenceProvider = (IQueryablePersistenceProvider)context.Application.
			                                                         	Factory.Resolve<IPersistenceProviderManager>().GetProvider();

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