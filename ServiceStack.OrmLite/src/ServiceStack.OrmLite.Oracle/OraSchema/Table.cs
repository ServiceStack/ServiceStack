using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Oracle.DbSchema;

namespace ServiceStack.OrmLite.Oracle
{
	public class Table : ITable
	{
		public Table()
		{
		}

        [Alias("TABLE_NAME")]
		public string Name { get; set; }

        [Alias("TABLE_SCHEMA")]
		public string Owner { get; set; }
	}
}

