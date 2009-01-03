using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests;
using ServiceStack.SakilaNHibernate.ServiceInterface.Translators;
using ServiceStack.Validation;

namespace ServiceStack.SakilaNHibernate.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class GetCustomersPort : IService
	{
		public object Execute(ICallContext context)
		{
			var request = context.Request.Get<GetCustomers>();
			var facade = context.Request.Get<ISakilaNHibernateServiceFacade>();

			try
			{
				var results = facade.GetCustomers(new CustomersRequest {
					CustomerIds = request.CustomerIds,
				});
				return new GetCustomersResponse {
					Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
				};
			}
			catch (ValidationException ve)
			{
				return new GetCustomersResponse { ResponseStatus = ResponseStatusTranslator.Instance.Parse(ve) };
			}
		}
	}
}
