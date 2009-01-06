using Sakila.ServiceModel.Version100.Operations;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaDb4oDemo.ServiceInterface.Version100
{
	public class GetCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetCustomers>();
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var results = provider.GetByIds<Sakila.DomainModel.Customer>(request.CustomerIds);

			return new GetCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
