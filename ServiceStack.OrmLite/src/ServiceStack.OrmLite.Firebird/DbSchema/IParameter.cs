using System;

namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public interface IParameter
	{
		string ProcedureName { get; set; }

		string Name { get; set; }

		Int16 Position { get; set; }

		Int16 PType { get; set; }

		string DbType { get; set; }

		Int32 Length { get; set; }


		Int32 Presicion { get; set; }

		Int32 Scale { get; set; }

		bool Nullable { get; set; }

		ParameterDirection Direction { get; }
	}
}

