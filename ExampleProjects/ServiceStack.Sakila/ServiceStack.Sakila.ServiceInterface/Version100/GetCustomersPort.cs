using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;
using ServiceStack.Sakila.ServiceInterface.Translators;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.ServiceInterface.Version100
{
	/// <summary>
	/// Get's users private information
	/// 
	/// Requires authentication.
	/// </summary>
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class GetCustomersPort : IService
	{
		public object Execute(CallContext context)
		{
			// Extract request DTO
			var request = context.Request.GetDto<GetCustomers>();

			// Retrieve the users
			var facade = context.Request.GetFacade<ISakilaServiceFacade>();
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
