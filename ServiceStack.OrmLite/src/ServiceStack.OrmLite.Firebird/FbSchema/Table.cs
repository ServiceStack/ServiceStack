using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird.DbSchema;

namespace ServiceStack.OrmLite.Firebird
{
	public class Table : ITable
	{
		public Table()
		{
		}

		[Alias("NAME")]
		public string Name { get; set; }

		[Alias("OWNER")]
		public string Owner { get; set; }
	}
}

