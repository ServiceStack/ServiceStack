using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.Validation
{
    /// <summary>
    /// The exception which is thrown when a validation error occurred.
    /// This validation is serialized in a extra clean and human-readable way by ServiceStack.
    /// </summary>
    public class ValidationError : ArgumentException, IResponseStatusConvertible
    {
        public string ErrorMessage { get; }

        public ValidationError(string errorCode)
            : this(errorCode, errorCode.SplitCamelCase())
        {
        }

        public ValidationError(ValidationErrorResult validationResult)
            : base(validationResult.ErrorMessage)
        {
            this.ErrorCode = validationResult.ErrorCode;
            this.ErrorMessage = validationResult.ErrorMessage;
            this.Violations = validationResult.Errors;
        }

        public ValidationError(ValidationErrorField validationError)
            : this(validationError.ErrorCode, validationError.ErrorMessage)
        {
            this.Violations.Add(validationError);
        }

        public ValidationError(string errorCode, string errorMessage)
            : base(errorMessage)
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
            this.Violations = new List<ValidationErrorField>();
        }

        /// <summary>
        /// Returns the first error code
        /// </summary>
        /// <value>The error code.</value>
        public string ErrorCode { get; }

        public override string Message
        {
            get
            {
                //If there is only 1 validation error than we just show the error message
                if (this.Violations.Count == 0)
                    return this.ErrorMessage;

                if (this.Violations.Count == 1)
                    return this.ErrorMessage ?? this.Violations[0].ErrorMessage;

                var sb = StringBuilderCache.Allocate()
                    .Append(this.ErrorMessage).AppendLine();
                foreach (var error in this.Violations)
                {
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                    {
                        var fieldLabel = error.FieldName != null ? $" [{error.FieldName}]" : null;
                        sb.Append($"\n  - {error.ErrorMessage}{fieldLabel}");
                    }
                    else
                    {
                        var fieldLabel = error.FieldName != null ? ": " + error.FieldName : null;
                        sb.Append($"\n  - {error.ErrorCode}{fieldLabel}");
                    }
                }
                return StringBuilderCache.ReturnAndFree(sb);
            }
        }

        public IList<ValidationErrorField> Violations { get; private set; }

        /// <summary>
        /// Used if we need to serialize this exception to XML
        /// </summary>
        /// <returns></returns>
        public string ToXml()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("<ValidationException>");
            foreach (var error in this.Violations)
            {
                sb.Append("<ValidationError>")
                    .Append($"<Code>{error.ErrorCode}</Code>")
                    .Append($"<Field>{error.FieldName}</Field>")
                    .Append($"<Message>{error.ErrorMessage}</Message>")
                    .Append("</ValidationError>");
            }
            sb.Append("</ValidationException>");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public static ValidationError CreateException(Enum errorCode)
        {
            return new ValidationError(errorCode.ToString());
        }

        public static ValidationError CreateException(Enum errorCode, string errorMessage)
        {
            return new ValidationError(errorCode.ToString(), errorMessage);
        }

        public static ValidationError CreateException(Enum errorCode, string errorMessage, string fieldName)
        {
            return CreateException(errorCode.ToString(), errorMessage, fieldName);
        }

        public static ValidationError CreateException(string errorCode)
        {
            return new ValidationError(errorCode);
        }

        public static ValidationError CreateException(string errorCode, string errorMessage)
        {
            return new ValidationError(errorCode, errorMessage);
        }

        public static ValidationError CreateException(string errorCode, string errorMessage, string fieldName)
        {
            var error = new ValidationErrorField(errorCode, fieldName, errorMessage);
            return new ValidationError(new ValidationErrorResult(new List<ValidationErrorField> { error }));
        }

        public static ValidationError CreateException(ValidationErrorField error)
        {
            return new ValidationError(error);
        }

        public static void ThrowIfNotValid(ValidationErrorResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationError(validationResult);
            }
        }

        public ResponseStatus ToResponseStatus()
        {
            return ResponseStatusUtils.CreateResponseStatus(ErrorCode, Message, Violations);
        }
    }
}