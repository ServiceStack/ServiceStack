using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Validation
{
	public class ValidationException : Exception
	{
		//required to re-create the Exception from a SOAP Fault
		public ValidationException(string message)
			: base(message)
		{
		}

		public ValidationException(ValidationResult validationResult)
			: base(validationResult.Message)
		{
			this.Violations = validationResult.Errors;
		}

		/// <summary>
		/// Returns the first error code
		/// </summary>
		/// <value>The error code.</value>
		public string ErrorCode
		{
			get
			{
				if (this.Violations.Count > 0)
				{
					return this.Violations[0].ErrorCode;
				}
				return null;
			}
		}

		public override string Message
		{
			get
			{
				var sb = new StringBuilder(base.Message).AppendLine();
				foreach (var error in this.Violations)
				{
					if (!string.IsNullOrEmpty(error.ErrorMessage))
					{
						sb.AppendFormat("\n  - {0} [{1}]", error.ErrorMessage, error.FieldName);
					}
					else
					{
						sb.AppendFormat("\n  - {0}: {1}", error.ErrorCode, error.FieldName);
					}					
				}
				return sb.ToString();
			}
		}

		public IList<ValidationError> Violations { get; private set; }

		/// <summary>
		/// Used if we need to serialize this exception to XML
		/// </summary>
		/// <returns></returns>
		public string ToXml()
		{
			var sb = new StringBuilder();
			sb.Append("<ValidationException>");
			foreach (ValidationError error in this.Violations)
			{
				sb.Append("<ValidationError>")
					.AppendFormat("<Code>{0}</Code>", error.ErrorCode)
					.AppendFormat("<Field>{0}</Field>", error.FieldName)
					.AppendFormat("<Message>{0}</Message>", error.ErrorMessage)
					.Append("</ValidationError>");
			}
			sb.Append("</ValidationException>");
			return sb.ToString();
		}

		public static void ThrowValidationError(string errorCode, string fieldName, string errorMessage)
		{
			var error = new ValidationError(errorCode, fieldName, errorMessage);
			throw new ValidationException(new ValidationResult(new List<ValidationError> { error }));
		}

		public static void ThrowValidationError(ValidationError error)
		{
			throw new ValidationException(new ValidationResult(new List<ValidationError> { error }));
		}

		public static void ThrowIfNotValid(ValidationResult validationResult)
		{
			if (!validationResult.IsValid)
			{
				throw new ValidationException(validationResult);
			}
		}
	}
}