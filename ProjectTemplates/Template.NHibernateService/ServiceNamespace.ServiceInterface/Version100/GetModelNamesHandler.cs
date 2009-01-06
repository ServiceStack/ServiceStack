using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class Get@ModelName@sHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<Get@ModelName@s>();
			var facade = context.Request.Get<I@ServiceName@Facade>();

			var results = facade.Get@ModelName@s(new @ModelName@sRequest {
				@ModelName@Ids = request.@ModelName@Ids,
			});
			return new Get@ModelName@sResponse {
				@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
