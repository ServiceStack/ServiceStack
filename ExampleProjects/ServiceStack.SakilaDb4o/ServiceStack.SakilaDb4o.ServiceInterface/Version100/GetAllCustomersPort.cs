using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaDb4o.ServiceInterface.Version100
{
	public class GetAllCustomersPort : IService
	{
		public object Execute(ICallContext context)
		{
			var provider = context.Operation.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();

			var results = provider.GetAll<Customer>();

			return new GetAllCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
