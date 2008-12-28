using System;

namespace ServiceStack.Validation
{
	public abstract class ValidationAttributeBase : Attribute
	{
		public string ErrorCode { get; set; }
		public string ErrorMessage { get; set; }
		public string ValidationGroup { get; set; }

		public virtual ValidationError ValidationError
		{
			get { return new ValidationError(ErrorCode, ErrorMessage, null); }
		}

		/// <summary>
		/// Validates the specified value.
		/// 
		/// If it is an error it returns an error code, otherwise null for success.
		/// 
		/// Use ValidationErrorCode.FieldIsRequired.ToString() to set the error code
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>ValidationErrorCode if InValid otherwise null</returns>
		public abstract string Validate(object value);
	}
}