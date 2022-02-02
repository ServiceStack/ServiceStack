using System.Data;
using System.Collections.Generic;

namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public interface ISchema<TTable, TColumn, TProcedure, TParameter>
		where TTable : ITable, new()
		where TColumn : IColumn, new()
		where TProcedure : IProcedure, new()
		where TParameter : IParameter, new()
	{
		IDbConnection Connection { set; }

		List<TTable> Tables { get; }

		TTable GetTable(string tableName);

		List<TColumn> GetColumns(string tableName);

		List<TColumn> GetColumns(TTable table);

		TProcedure GetProcedure(string name);

		List<TParameter> GetParameters(TProcedure procedure);

		List<TParameter> GetParameters(string procedureName);
	}
}
