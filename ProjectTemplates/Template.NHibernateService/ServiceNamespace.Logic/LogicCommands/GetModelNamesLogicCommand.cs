using System.Collections.Generic;
using @DomainModelNamespace@;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class Get@ModelName@sLogicCommand : LogicCommandBase<IList<@ModelName@>>
	{
		public @ModelName@sRequest Request { get; set; }

		public override IList<@ModelName@> Execute()
		{
			return Provider.GetByIds<@ModelName@>(this.Request.@ModelName@Ids);
		}
	}
}