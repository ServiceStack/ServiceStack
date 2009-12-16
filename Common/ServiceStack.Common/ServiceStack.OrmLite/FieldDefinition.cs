using System;
using System.Reflection;

namespace ServiceStack.OrmLite
{
	public class FieldDefinition
	{
		public string Name { get; set; }

		public Type FieldType { get; set; }

		public PropertyInfo PropertyInfo { get; set; }

		public bool IsPrimaryKey { get; set; }

		public bool AutoIncrement { get; set; }

		public bool IsNullable { get; set; }

		public bool IsIndexed { get; set; }

		public bool IsUnique { get; set; }

		public int? FieldLength { get; set; }

		public string DefaultValue { get; set; }

		public Func<object, Type, object> ConvertValueFn { get; set; }

		public Func<object, Type, string> QuoteValueFn { get; set; }

		public IPropertyInvoker PropertyInvoker { get; set; }

		public Func<object, object> GetValueFn { get; set; }

		public void SetValue(object onInstance, object withValue)
		{
			PropertyInvoker.SetPropertyValue(this.PropertyInfo, FieldType, onInstance, withValue);
		}

		public string GetQuotedValue(object fromInstance)
		{
			var value = PropertyInvoker.GetPropertyValue(this.PropertyInfo, fromInstance);
			return QuoteValueFn(value, FieldType);
		}
	}
}