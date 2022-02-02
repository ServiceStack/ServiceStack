using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird.DbSchema;

namespace ServiceStack.OrmLite.Firebird
{
	public class Parameter : IParameter
	{
		public Parameter()
		{
			Nullable = true;
		}

		[Alias("PROCEDURE_NAME")]
		public string ProcedureName { get; set; }

		[Alias("PARAMETER_NAME")]
		public string Name { get; set; }

		[Alias("PARAMETER_NUMBER")]
		public Int16 Position { get; set; }

		[Alias("PARAMETER_TYPE")]
		public Int16 PType { get; set; }

		[Alias("FIELD_TYPE")]
		public string DbType { get; set; }

		[Alias("FIELD_LENGTH")]
		public int Length { get; set; }


		[Alias("FIELD_PRECISION")]
		public int Presicion { get; set; }


		[Alias("FIELD_SCALE")]
		public int Scale { get; set; }

		[Ignore]
		public bool Nullable
		{
			get;
			set;
		}

		[Ignore]
		public ParameterDirection Direction
		{
			get
			{
				return PType == 0 ? ParameterDirection.Input : ParameterDirection.Ouput;
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
					case "INT64":
						t = Nullable ? typeof(Int64?) : typeof(Int64);
						break;

					case "BLOB":
						t = Nullable ? typeof(Byte?[]) : typeof(Byte[]);
						break;

					case "DOUBLE PRECISION":
						t = Nullable ? typeof(Double?) : typeof(Double);
						break;

					case "FLOAT":
						t = Nullable ? typeof(float?) : typeof(float);
						break;

					case "DECIMAL":
					case "NUMERIC":
						t = Nullable ? typeof(Decimal?) : typeof(Decimal);
						break;

					case "SMALLINT":
					case "SHORT":
						t = Nullable ? typeof(Int16?) : typeof(Int16);
						break;

					case "DATE":
					case "TIME":
					case "TIMESTAMP":
						t = Nullable ? typeof(DateTime?) : typeof(DateTime);
						break;

					case "INTEGER":
					case "LONG":
						t = Nullable ? typeof(Int32?) : typeof(Int32);
						break;


					case "VARCHAR":
					case "CHAR":
					case "VARYING":
					case "TEXT":
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

