using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModelTranslators.Version100.ServiceToDomain;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaDb4o.ServiceInterface.Translators;

namespace ServiceStack.SakilaDb4o.ServiceInterface.Version100
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
			var provider = context.Operation.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();

			var customer = CustomerFromDtoTranslator.Instance.Parse(request.Customer);

			var response = new StoreCustomerResponse {
				ResponseStatus = ResponseStatusTranslator.Instance.Parse(customer.Validate())
			};

			if (response.ResponseStatus.ErrorCode == null)
			{
				using (var transaction = provider.BeginTransaction())
				{
					provider.Save(customer);					
					transaction.Commit();
				}
			}

			return response;
		}

	}
}
