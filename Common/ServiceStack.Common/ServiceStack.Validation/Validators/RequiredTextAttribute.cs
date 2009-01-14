using System;

namespace ServiceStack.Validation.Validators
{
	[AttributeUsage(AttributeTargets.Property)]
	public class RequiredTextAttribute : ValidationAttributeBase
	{
		public override string Validate(object value)
		{
			var text = (string)value;
			return !string.IsNullOrEmpty(text) ? null : ValidationErrorCodes.FieldIsRequired.ToString();
		}
	}
}