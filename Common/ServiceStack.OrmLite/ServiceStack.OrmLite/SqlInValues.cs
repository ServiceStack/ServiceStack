using System.Collections;

namespace ServiceStack.OrmLite
{
	public class SqlInValues
	{
		private readonly IEnumerable values;

		public SqlInValues(IEnumerable values)
		{
			this.values = values;
		}

		public string ToSqlInString()
		{
			return OrmLiteUtilExtensions.SqlJoin(values);
		}
	}
}