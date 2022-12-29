using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird.DbSchema;

namespace ServiceStack.OrmLite.Firebird
{
	public class Procedure : IProcedure
	{
		public Procedure()
		{
		}

		[Alias("NAME")]
		public string Name { get; set; }

		[Alias("OWNER")]
		public string Owner { get; set; }

		[Alias("INPUTS")]
		public Int16 Inputs { get; set; }

		[Alias("OUTPUTS")]
		public Int16 Outputs { get; set; }

		[Ignore]
		public ProcedureType Type
		{
			get
			{
				return Outputs == 0 ? ProcedureType.Executable : ProcedureType.Selectable;
			}
		}
	}
}

