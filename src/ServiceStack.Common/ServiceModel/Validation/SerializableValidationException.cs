using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation
{
    /// <summary>
    /// The exception which is thrown when a validation error occured.
    /// This validation is serialized in a extra clean and human-readable way by ServiceStack.
    /// </summary>
	public class SerializableValidationException : Exception
	{
		private readonly string errorCode;
		public string ErrorMessage { get; private set; }

		public SerializableValidationException(string errorCode)
			: this(errorCode, errorCode.SplitCamelCase())
		{
		}

		public SerializableValidationException(SerializableValidationResult validationResult)
			: base(validationResult.ErrorMessage)
		{
			this.errorCode = validationResult.ErrorCode;
			this.ErrorMessage = validationResult.ErrorMessage;
			this.Violations = validationResult.Errors;
		}

		public SerializableValidationException(SerializableValidationError validationError)
			: this(validationError.ErrorCode, validationError.ErrorMessage)
		{
			this.Violations.Add(validationError);
		}

		public SerializableValidationException(string errorCode, string errorMessage)
			: base(errorMessage)
		{
			this.errorCode = errorCode;
			this.ErrorMessage = errorMessage;
			this.Violations = new List<SerializableValidationError>();
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

				if (this.Violations.Count == 1)
					return this.ErrorMessage ?? this.Violations[0].ErrorMessage;

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

		public IList<SerializableValidationError> Violations { get; private set; }

		/// <summary>
		/// Used if we need to serialize this exception to XML
		/// </summary>
		/// <returns></returns>
		public string ToXml()
		{
			var sb = new StringBuilder();
			sb.Append("<ValidationException>");
			foreach (SerializableValidationError error in this.Violations)
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

		public static SerializableValidationException CreateException(Enum errorCode)
		{
			return new SerializableValidationException(errorCode.ToString());
		}

		public static SerializableValidationException CreateException(Enum errorCode, string errorMessage)
		{
			return new SerializableValidationException(errorCode.ToString(), errorMessage);
		}

		public static SerializableValidationException CreateException(Enum errorCode, string errorMessage, string fieldName)
		{
			return CreateException(errorCode.ToString(), errorMessage, fieldName);
		}

		public static SerializableValidationException CreateException(string errorCode)
		{
			return new SerializableValidationException(errorCode);
		}

		public static SerializableValidationException CreateException(string errorCode, string errorMessage)
		{
			return new SerializableValidationException(errorCode, errorMessage);
		}

		public static SerializableValidationException CreateException(string errorCode, string errorMessage, string fieldName)
		{
			var error = new SerializableValidationError(errorCode, fieldName, errorMessage);
			return new SerializableValidationException(new SerializableValidationResult(new List<SerializableValidationError> { error }));
		}

		public static SerializableValidationException CreateException(SerializableValidationError error)
		{
			return new SerializableValidationException(error);
		}

		public static void ThrowIfNotValid(SerializableValidationResult validationResult)
		{
			if (!validationResult.IsValid)
			{
				throw new SerializableValidationException(validationResult);
			}
		}
	}
}