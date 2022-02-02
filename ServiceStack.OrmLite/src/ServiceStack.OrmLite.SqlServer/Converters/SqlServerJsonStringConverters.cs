using System;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerJsonStringConverter : SqlServerStringConverter
	{
		// json string to object
		public override object FromDbValue(Type fieldType, object value)
		{
			if (value is string raw && fieldType.HasAttributeCached<SqlJsonAttribute>())
				return JsonSerializer.DeserializeFromString(raw, fieldType);

			return base.FromDbValue(fieldType, value);
		}

		// object to json string
		public override object ToDbValue(Type fieldType, object value)
		{
			if (value.GetType().HasAttributeCached<SqlJsonAttribute>())
				return JsonSerializer.SerializeToString(value, value.GetType());

			return base.ToDbValue(fieldType, value);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class SqlJsonAttribute : Attribute
	{ }
}