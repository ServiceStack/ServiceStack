using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceNamespace@.ServiceInterface.Translators;
using ServiceStack.Validation;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class Get@ModelName@sPort : IService
	{
		public object Execute(ICallContext context)
		{
			var request = context.Request.Get<Get@ModelName@s>();
			var facade = context.Request.Get<I@ServiceName@Facade>();

			try
			{
				var results = facade.Get@ModelName@s(new @ModelName@sRequest {
					@ModelName@Ids = request.@ModelName@Ids,
				});
				return new Get@ModelName@sResponse {
					@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
				};
			}
			catch (ValidationException ve)
			{
				return new Get@ModelName@sResponse { ResponseStatus = ResponseStatusTranslator.Instance.Parse(ve) };
			}
		}
	}
}
