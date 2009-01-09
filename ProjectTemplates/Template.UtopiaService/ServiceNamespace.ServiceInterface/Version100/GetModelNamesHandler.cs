using @ServiceModelNamespace@.Version100.Operations;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public class Get@ModelName@sHandler : HandlerBase
	{
		public override object Execute(@DatabaseName@OperationContext context)
		{
			var results = context.Provider.GetAll<DomainModel.@ModelName@>();

			return new Get@ModelName@sResponse {
				@ModelName@s = @ModelName@.ParseAll(results)
			};
		}
	}
}