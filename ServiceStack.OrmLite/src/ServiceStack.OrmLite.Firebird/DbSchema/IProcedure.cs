using System;

namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public interface IProcedure
	{
		string Name { get; set; }
		
		string Owner { get; set; }
		
		Int16 Inputs { get; set; }
		
		Int16 Outputs { get; set; }
		
		ProcedureType Type { get; }
	}
}

