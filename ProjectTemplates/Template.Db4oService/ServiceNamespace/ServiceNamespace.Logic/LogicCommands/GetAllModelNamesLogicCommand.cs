using System.Collections.Generic;
using @DomainModelNamespace@;
using ServiceStack.Common.Extensions;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class GetAll@ModelName@sLogicCommand : LogicCommandBase<IList<@ModelName@>>
	{
		public override IList<@ModelName@> Execute()
		{
			var customers = Provider.GetAll<@ModelName@>();
			return customers;
		}
	}
}