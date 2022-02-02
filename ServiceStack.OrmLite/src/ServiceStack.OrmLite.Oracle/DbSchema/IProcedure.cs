using System;

namespace ServiceStack.OrmLite.Oracle.DbSchema
{
	public interface IProcedure
	{
        string ProcedureName { get; set; }

        string FunctionName { get; set; }

		string Owner { get; set; }

        string ObjectType { get; set; }
		
		//Int16 Inputs { get; set; }
		
		//Int16 Outputs { get; set; }
		
		ProcedureType Type { get; }
	}
}

