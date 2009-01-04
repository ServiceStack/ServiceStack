using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaDb4o.ServiceInterface.Translators;

namespace ServiceStack.SakilaDb4o.ServiceInterface.Version100
{
	public class StoreCustomerPort : IService
	{

		public object Execute(ICallContext context)
		{
			var request = context.Request.Get<StoreCustomer>();
			var provider = context.Operation.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();

			var customer = CustomerFromDtoTranslator.Instance.Parse(request.Customer);

			var response = new StoreCustomerResponse {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(customer.Validate())
			};

			//Only store valid Customers. 			
			if (response.ResponseStatus.ErrorCode == null)
			{
				//If possible this 'Write' request should be stored 
				//and executed Asynchronously after the response is returned to the client
				using (var transaction = provider.BeginTransaction())
				{
					provider.Store(customer);
					transaction.Commit();
				}
			}

			return response;
		}

	}
}
