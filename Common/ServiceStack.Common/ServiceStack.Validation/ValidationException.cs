using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation
{
	public class ValidationException : Exception
	{
		private readonly string errorCode;
		public string ErrorMessage { get; private set; }

		public ValidationException(string errorCode)
			: this(errorCode, errorCode.SplitCamelCase())
		{
		}

		public ValidationException(ValidationResult validationResult)
			: this(validationResult.Message)
		{
			this.Violations = validationResult.Errors;
		}

		public ValidationException(string errorCode, string errorMessage)
			: base(errorMessage)
		{
			this.errorCode = errorCode;
			this.ErrorMessage = errorMessage;
			this.Violations = new List<ValidationError>();
		}

		/// <summary>
		/// Returns the first error code
		/// </summary>
		/// <value>The error code.</value>
		public string ErrorCode
		{
			get
			{
				return this.errorCode;
			}
		}

		public override string Message
		{
			get
			{
				//If there is only 1 validation error than we just show the error message
				if (this.Violations.Count == 0)
					return this.ErrorMessage;

				if (this.Violations.Count == 1 && this.ErrorMessage == null)
					return this.Violations[0].ErrorMessage;

				var sb = new StringBuilder(this.ErrorMessage).AppendLine();
				foreach (var error in this.Violations)
				{
					if (!string.IsNullOrEmpty(error.ErrorMessage))
					{
						var fieldLabel = error.FieldName != null ? string.Format(" [{0}]", error.FieldName) : null;
						sb.AppendFormat("\n  - {0}{1}", error.ErrorMessage, fieldLabel);
					}
					else
					{
						var fieldLabel = error.FieldName != null ? ": " + error.FieldName : null;
						sb.AppendFormat("\n  - {0}{1}", error.ErrorCode, fieldLabel);
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

		public static ValidationException CreateException(string errorCode)
		{
			return new ValidationException(errorCode);
		}

		public static ValidationException CreateException(string errorCode, string errorMessage)
		{
			return new ValidationException(errorCode, errorMessage);
		}

		public static ValidationException CreateException(string errorCode, string errorMessage, string fieldName)
		{
			var error = new ValidationError(errorCode, fieldName, errorMessage);
			return new ValidationException(new ValidationResult(new List<ValidationError> { error }));
		}

		public static ValidationException CreateException(ValidationError error)
		{
			return new ValidationException(new ValidationResult(new List<ValidationError> { error }));
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