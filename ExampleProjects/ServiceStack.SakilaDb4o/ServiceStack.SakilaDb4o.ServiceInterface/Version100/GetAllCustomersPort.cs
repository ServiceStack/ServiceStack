using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaDb4o.ServiceInterface.Translators;
using ServiceStack.Validation;

namespace ServiceStack.SakilaDb4o.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
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
