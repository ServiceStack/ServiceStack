using System;

namespace ServiceStack.Validation.Validators
{
	public abstract class ValidationAttributeBase : Attribute
	{
		public string ErrorCode { get; set; }
		public string ErrorMessage { get; set; }
		public string ValidationGroup { get; set; }

		public virtual ValidationError ValidationError
		{
			get { return new ValidationError(this.ErrorCode, this.ErrorMessage, null); }
		}

		/// <summary>
		/// Validates the specified value.
		/// 
		/// If it is an error it returns an error code, otherwise null for success.
		/// 
		/// Use ValidationErrorCode.Required.ToString() to set the error code
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>ValidationErrorCode if InValid otherwise null</returns>
		public abstract string Validate(object value);
	}
}