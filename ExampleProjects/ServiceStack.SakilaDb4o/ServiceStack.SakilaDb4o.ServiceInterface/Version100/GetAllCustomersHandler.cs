using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
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
			var provider = context.Application.Get<IPersistenceProviderManager>().CreateProvider();

			var results = provider.GetAll<Customer>();

			return new GetAllCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
