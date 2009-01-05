using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class GetAll@ModelName@sHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var provider = context.Application.Get<IPersistenceProviderManager>().CreateProvider();

			var results = provider.GetAll<@ModelName@>();

			return new GetAll@ModelName@sResponse {
				@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
