using System.Collections.Generic;
using @DomainModelNamespace@;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class GetAll@ModelName@sLogicCommand : LogicCommandBase<IList<@ModelName@>>
	{
		public override IList<@ModelName@> Execute()
		{
			return Provider.GetAll<@ModelName@>();
		}
	}
}