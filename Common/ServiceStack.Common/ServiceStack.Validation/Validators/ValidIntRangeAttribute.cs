using System;

namespace ServiceStack.Validation.Validators
{
	/// <summary>
	/// Validates string is a valid email address.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidIntRangeAttribute : ValidationAttributeBase
	{
		public int From { get; set; }
		public int To { get; set; }

		public override string Validate(object value)
		{
			var intVal = (int)value;

			var isValid = intVal >= From && intVal <= To;						

			if (!isValid)
			{
				this.ErrorMessage = string.Format("Value '{0}' is not within the valid range of {1} - {2}", 
					intVal, From, To);
				
				return ValidationErrorCodes.OutOfRange.ToString();
			}

			return null;
		}
	}
}