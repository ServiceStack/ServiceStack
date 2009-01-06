using Sakila.ServiceModel.Version100.Operations;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaDb4o.ServiceInterface.Version100
{
	public class GetAllCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var results = provider.GetAll<Sakila.DomainModel.Customer>();

			return new GetAllCustomersResponse {
				Customers = Customer.ParseAll(results)
			};
		}
	}
}
