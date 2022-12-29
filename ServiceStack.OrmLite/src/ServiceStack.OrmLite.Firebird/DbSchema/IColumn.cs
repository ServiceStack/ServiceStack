using System;

namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public interface IColumn
	{
		string Name { get; set; }

		int Position { get; set; }

		string DbType { get; set; }

		int Length { get; set; }

		int Presicion { get; set; }

		int Scale { get; set; }

		bool Nullable { get; set; }

		string Description { get; set; }

		string TableName { get; set; }

		bool IsPrimaryKey { get; set; }

		bool IsUnique { get; set; }

		string Sequence { get; set; }

		bool IsComputed { get; set; }

		bool AutoIncrement { get; set; }

		Type NetType { get; }
	}
}

