using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaNHibernate.ServiceInterface.Version100
{
	public class GetCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetCustomers>();
			var facade = context.Request.Get<ISakilaNHibernateServiceFacade>();

			var results = facade.GetCustomers(new CustomersRequest {
				CustomerIds = request.CustomerIds,
			});
			return new GetCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
