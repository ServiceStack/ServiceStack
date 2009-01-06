using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaDb4oDemo.ServiceInterface.Version100
{
	public class GetAllCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var results = provider.GetAll<Customer>();

			return new GetAllCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
