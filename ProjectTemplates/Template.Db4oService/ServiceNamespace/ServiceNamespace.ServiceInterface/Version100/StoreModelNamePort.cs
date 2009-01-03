using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.ServiceToDomain;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
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
			var provider = context.Operation.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();

			var customer = @ModelName@FromDtoTranslator.Instance.Parse(request.@ModelName@);

			var response = new Store@ModelName@Response {
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
