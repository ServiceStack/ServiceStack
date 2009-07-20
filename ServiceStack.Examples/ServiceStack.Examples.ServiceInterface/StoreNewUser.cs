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
	 * It lists all the classes required to implement the 'StoreNewUser' Service. 	
	 */

	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class StoreNewUser
	{
		[DataMember]
		public string UserName { get; set; }

		[DataMember]
		public string Email { get; set; }

		[DataMember]
		public string Password { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class StoreNewUserResponse
	{
		public StoreNewUserResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public long UserId { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}


	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// </summary>
	[Port(typeof(StoreNewUser))]
	public class StoreNewUserHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<StoreNewUser>();

			//Get the persistence provider registered in the AppHost
			var persistenceProvider = (IQueryablePersistenceProvider)context.Application.
				Factory.Resolve<IPersistenceProviderManager>().GetProvider();

			var existingUsers = persistenceProvider.Query<User>(user =>
				user.UserName == request.UserName || user.Email == request.Email).ToList();

			if (existingUsers.Count > 0)
			{
				return new StoreNewUserResponse {
					ResponseStatus = new ResponseStatus { ErrorCode = "UserAlreadyExists" }
				};
			}

			var newUser = new User { UserName = request.UserName, Email = request.Email, Password = request.Password };

			persistenceProvider.Store(newUser);

			return new StoreNewUserResponse { UserId = newUser.Id };
		}
	}

}
