using @ServiceModelNamespace@.Version100.Operations;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class Get@ModelName@sHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<Get@ModelName@s>();
			var provider = context.Application.Get<IPersistenceProviderManager>().GetProvider();

			var results = provider.GetByIds<@DomainModelNamespace@.@ModelName@>(request.@ModelName@Ids);

			return new Get@ModelName@sResponse {
				@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
