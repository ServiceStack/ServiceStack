using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.ServiceToDomain;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface.Translators;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class Store@ModelName@Handler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<Store@ModelName@>();
			var facade = context.Request.Get<I@ServiceName@Facade>();

			var customers = @ModelName@FromDtoTranslator.Instance.Parse(request.@ModelName@);

			IValidatableCommand<bool> command;

			using (var initOnlyContext = facade.AcquireInitContext(InitOptions.InitialiseOnly))
			{
				facade.Store@ModelName@(customers);
				command = (IValidatableCommand<bool>)initOnlyContext.InitialisedObject;
			}

			var response = new Store@ModelName@Response {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(command.Validate())
			};

			//Only store valid @ModelName@s. 			
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
