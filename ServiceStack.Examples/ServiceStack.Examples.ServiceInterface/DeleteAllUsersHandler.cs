using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// </summary>
	[Port(typeof(DeleteAllUsers))]
	public class DeleteAllUsersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<DeleteAllUsers>();

			//Get the persistence provider registered in the AppHost
			var persistenceProvider = context.Application.Factory.Resolve<IPersistenceProviderManager>().GetProvider();

			var deleteUsers = persistenceProvider.GetAll<User>();

			persistenceProvider.DeleteAll(deleteUsers);

			return new DeleteAllUsersResponse();
		}
	}
}