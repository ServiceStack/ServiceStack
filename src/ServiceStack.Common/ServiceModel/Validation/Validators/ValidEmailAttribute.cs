using System;
using System.Text.RegularExpressions;

namespace ServiceStack.Validation.Validators
{
	/// <summary>
	/// Validates string is a valid email address.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidEmailAttribute : ValidationAttributeBase
	{
		const string ValidEmailPattern = 
				@"^([a-zA-Z0-9_\-\.]+)@
			((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))
			([a-zA-Z0-9]{1,63})(\]?)$";

		static readonly Regex ValidEmail = new Regex(ValidEmailPattern,
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		public override string Validate(object value)
		{
			var text = (string)value;
			return text != null && ValidEmail.IsMatch(text) ? null : ValidationErrorCodes.EmailAddressIsNotValid.ToString();
		}
	}
}