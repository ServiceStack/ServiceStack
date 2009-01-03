using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.ServiceToDomain;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface.Translators;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class Store@ModelName@Port : IService
	{
		/// <summary>
		/// Used by Json and Soap requests if this service *is not* a 'IXElementService'
		/// </summary>
		/// <returns></returns>
		public object Execute(ICallContext context)
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
			if (response.ResponseStatus.ErrorCode == null)
			{
				//This needs to be synchronous as the client will attempt to auto-login after creating a new user
				command.Execute();
			}
			return response;
		}
	}
}
