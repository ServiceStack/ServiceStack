using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.ServiceInterface.Translators;

namespace ServiceStack.Sakila.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class StoreCustomerPort : IService
	{
		/// <summary>
		/// Used by Json and Soap requests if this service *is not* a 'IXElementService'
		/// </summary>
		/// <returns></returns>
		public object Execute(CallContext context)
		{
			var request = context.Request.GetDto<StoreCustomer>();
			var facade = context.Request.GetFacade<ISakilaServiceFacade>();

			var customers = CustomerFromDtoTranslator.Instance.Parse(request.Customer);

			IValidatableCommand<bool> command;

			using (var initOnlyContext = facade.AcquireInitContext(InitOptions.InitialiseOnly))
			{
				facade.StoreCustomer(customers);
				command = (IValidatableCommand<bool>)initOnlyContext.InitialisedObject;
			}

			var response = new StoreCustomersResponse {
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
