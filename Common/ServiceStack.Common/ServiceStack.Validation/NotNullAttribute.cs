using System;
using ServiceStack.Validation;

namespace ServiceStack.Validation
{
	/// <summary>
	/// Tests if the field is not null or not default(T) for value types
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : ValidationAttributeBase
	{
		public override string Validate(object value)
		{
			if (value == null) return ValidationErrorCodes.FieldIsRequired.ToString();

			var valueType = value.GetType();
			if (valueType.IsValueType)
			{
				var defaultValue = Activator.CreateInstance(valueType);
				return !defaultValue.Equals(value) ? null : ValidationErrorCodes.FieldIsRequired.ToString(); //not the same as '=='
			}
			return null;
		}
	}
}