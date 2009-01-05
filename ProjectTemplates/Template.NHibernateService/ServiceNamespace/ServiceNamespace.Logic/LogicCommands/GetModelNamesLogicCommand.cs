using System.Collections.Generic;
using @DomainModelNamespace@;
using ServiceStack.Common.Extensions;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceNamespace@.Logic.Translators.DataToDomain;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class Get@ModelName@sLogicCommand : LogicCommandBase<List<@ModelName@>>
	{
		public @ModelName@sRequest Request { get; set; }

		public override List<@ModelName@> Execute()
		{
			var db@ModelName@s = Provider.GetByIds<DataAccess.DataModel.@ModelName@>(
				this.Request.@ModelName@Ids.ConvertAll(x => (ushort)x));

			return @ModelName@FromDataTranslator.Instance.ParseAll(db@ModelName@s);
		}
	}
}