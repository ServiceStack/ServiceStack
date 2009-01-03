using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@Translators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
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
			var provider = context.Operation.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();

			var results = provider.GetByIds<@ModelName@>(request.@ModelName@Ids);

			return new Get@ModelName@sResponse {
				@ModelName@s = @ModelName@ToDtoTranslator.Instance.ParseAll(results)
			};
		}
	}
}
