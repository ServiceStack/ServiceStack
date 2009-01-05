using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.ServiceInterface.Translators;

namespace ServiceStack.SakilaNHibernate.ServiceInterface.Version100
{
	public class StoreCustomerHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<StoreCustomer>();
			var facade = context.Request.Get<ISakilaNHibernateServiceFacade>();

			var customers = CustomerFromDtoTranslator.Instance.Parse(request.Customer);

			IValidatableCommand<bool> command;

			using (var initOnlyContext = facade.AcquireInitContext(InitOptions.InitialiseOnly))
			{
				facade.StoreCustomer(customers);
				command = (IValidatableCommand<bool>)initOnlyContext.InitialisedObject;
			}

			var response = new StoreCustomerResponse {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(command.Validate())
			};

			//Only store valid Customers. 			
			if (response.ResponseStatus.ErrorCode == null)
			{
				//Ideally this 'Store' request should be persisted before execution
				//and executed Asynchronously after the response is returned to the client
				command.Execute();
			}
			return response;
		}
	}
}
