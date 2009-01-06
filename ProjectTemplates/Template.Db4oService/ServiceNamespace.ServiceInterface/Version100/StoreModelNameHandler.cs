using @ServiceModelNamespace@.Version100.Operations;
using @ServiceModelNamespace@Translators.Version100.ServiceToDomain;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using @ServiceNamespace@.ServiceInterface.Translators;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class Store@ModelName@Handler : IService
	{

		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<Store@ModelName@>();
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var customer = @ModelName@FromDtoTranslator.Instance.Parse(request.@ModelName@);

			var response = new Store@ModelName@Response {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(customer.Validate())
			};

			//Only store valid @ModelName@s. 			
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
