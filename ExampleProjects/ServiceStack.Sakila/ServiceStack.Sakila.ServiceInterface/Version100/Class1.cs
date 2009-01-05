using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.ServiceInterface.Translators;
using ServiceStack.ServiceInterface;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.ServiceInterface.Version100
{
	public class GetAllCustomersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var facade = context.Request.Get<ISakilaServiceFacade>();

			try
			{
				var results = facade.GetAllCustomers();
				return new GetAllCustomersResponse {
					Customers = CustomerToDtoTranslator.Instance.ParseAll(results)
				};
			}
			catch (ValidationException ve)
			{
				return new GetAllCustomersResponse { ResponseStatus = ResponseStatusTranslator.Instance.Parse(ve) };
			}
		}
	}
}
