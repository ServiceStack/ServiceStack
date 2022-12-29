using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Oracle.DbSchema;

namespace ServiceStack.OrmLite.Oracle
{
	public class Column : IColumn
	{
		public Column()
		{
		}

        [Alias("column_name")]
		public string Name { get; set; }

        [Alias("column_id")]
		public int Position { get; set; }


        [Alias("data_type")]
		public string DbType { get; set; }


        [Alias("data_length")]
		public int Length { get; set; }


        [Alias("data_precision")]
		public int Presicion { get; set; }


        [Alias("data_scale")]
		public int Scale { get; set; }

        [Alias("nullable")]
		public bool Nullable { get; set; }

		//[Alias("DESCRIPTION")]
		//public string Description { get; set; }

        [Alias("table_name")]
		public string TableName { get; set; }

		[Ignore]
		public bool IsPrimaryKey { get; set; }

		[Ignore]
		public bool IsUnique { get; set; }

		[Ignore]
		public string Sequence { get; set; }

		[Ignore]
		public bool IsComputed { get; set; }

		[Ignore]
		public bool AutoIncrement
		{
			get { return !string.IsNullOrEmpty(Sequence); }
			set
			{
				;
			}
		}
		[Ignore]
		public Type NetType
		{
			get
			{
				Type t;
				switch (DbType)
				{
					case "BIGINT":
						t = Nullable ? typeof(Int64?) : typeof(Int64);
						break;

					case "BLOB":
						t = Nullable ? typeof(Byte?[]) : typeof(Byte[]);
						break;

					case "CHAR":
						t = typeof(string);
						break;

					case "DATE":
						t = Nullable ? typeof(DateTime?) : typeof(DateTime);
						break;

					case "DECIMAL":
						t = Nullable ? typeof(Decimal?) : typeof(Decimal);
						break;

					case "DOUBLE PRECISION":
						t = Nullable ? typeof(Double?) : typeof(Double);
						break;

					case "FLOAT":
						t = Nullable ? typeof(float?) : typeof(float);
						break;

					case "NUMERIC":
						t = Nullable ? typeof(Decimal?) : typeof(Decimal);
						break;

					case "SMALLINT":
						t = Nullable ? typeof(Int16?) : typeof(Int16);
						break;

					case "TIME":
						t = Nullable ? typeof(DateTime?) : typeof(DateTime);
						break;

					case "TIMESTAMP":
						t = Nullable ? typeof(DateTime?) : typeof(DateTime);
						break;

					case "TEXT":
						t = typeof(string);
						break;

					case "INTEGER":
						t = Nullable ? typeof(Int32?) : typeof(Int32);
						break;

					case "VARCHAR":
						t = typeof(string);
						break;
					case "GUID":
						t = Nullable ? typeof(Guid?) : typeof(Guid);
						break;
					default:
						t = typeof(string);
						break;
				}

				return t;
			}
		}

	}
}

