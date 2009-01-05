using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using @ServiceNamespace@.Logic.LogicInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class GetAll@ModelName@sHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var facade = context.Request.Get<I@ServiceName@Facade>();

			var results = facade.GetAll@ModelName@s();

			return new GetAll@ModelName@sResponse {
				@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
