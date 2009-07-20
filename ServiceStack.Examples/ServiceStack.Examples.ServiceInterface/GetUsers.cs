using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{

	/* Below is a simple example on how to create a simple Web Service.
	 * It lists all the classes required to implement the 'GetUsers' Service. 	
	 */

	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetUsers
	{
		[DataMember]
		public ArrayOfLong UserIds { get; set; }

		[DataMember]
		public ArrayOfString UserNames { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetUsersResponse
	{
		public GetUsersResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ArrayOfUser Users { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}


	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
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