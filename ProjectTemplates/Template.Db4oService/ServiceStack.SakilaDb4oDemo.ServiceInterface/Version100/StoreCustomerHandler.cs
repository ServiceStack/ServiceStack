using Sakila.ServiceModel.Version100.Operations;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaDb4oDemo.ServiceInterface.Translators;

namespace ServiceStack.SakilaDb4oDemo.ServiceInterface.Version100
{
	public class StoreCustomerHandler : IService
	{

		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<StoreCustomer>();
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var customer = CustomerFromDtoTranslator.Instance.Parse(request.Customer);

			var response = new StoreCustomerResponse {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(customer.Validate())
			};

			//Only store valid Customers. 			
			if (response.ResponseStatus.ErrorCode == null)
			{
				//Ideally this 'Store' request should be persisted before execution
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
