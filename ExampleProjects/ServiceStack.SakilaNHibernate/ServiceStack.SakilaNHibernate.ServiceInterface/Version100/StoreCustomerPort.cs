using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.ServiceInterface.Translators;

namespace ServiceStack.SakilaNHibernate.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class StoreCustomerPort : IService
	{
		/// <summary>
		/// Used by Json and Soap requests if this service *is not* a 'IXElementService'
		/// </summary>
		/// <returns></returns>
		public object Execute(ICallContext context)
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
			if (response.ResponseStatus.ErrorCode == null)
			{
				//This needs to be synchronous as the client will attempt to auto-login after creating a new user
				command.Execute();
			}
			return response;
		}
	}
}
