using System.Collections.Generic;
using @DomainModelNamespace@;
using ServiceStack.LogicFacade;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.Logic.LogicInterface
{
	public interface I@ServiceName@Facade : ILogicFacade
	{
		IList<@ModelName@> GetAll@ModelName@s();

		IList<@ModelName@> Get@ModelName@s(@ModelName@sRequest request);

		void Store@ModelName@(@ModelName@ entity);
	}
}