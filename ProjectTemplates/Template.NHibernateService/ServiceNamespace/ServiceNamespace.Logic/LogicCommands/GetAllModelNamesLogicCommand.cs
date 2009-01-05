using System.Collections.Generic;
using @DomainModelNamespace@;
using ServiceStack.Common.Extensions;
using @ServiceNamespace@.Logic.Translators.DataToDomain;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class GetAll@ModelName@sLogicCommand : LogicCommandBase<List<@ModelName@>>
	{
		public override List<@ModelName@> Execute()
		{
			var db@ModelName@s = Provider.GetAll<DataAccess.DataModel.@ModelName@>();
			return @ModelName@FromDataTranslator.Instance.ParseAll(db@ModelName@s);
		}
	}
}