using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;

namespace ServiceStack.SakilaNHibernate.ServiceInterface.Version100
{
	public class GetAllCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var facade = context.Request.Get<ISakilaNHibernateServiceFacade>();

			var results = facade.GetAllCustomers();

			return new GetAllCustomersResponse {
				Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
