using System;
using System.Collections.Generic;

namespace ServiceStack.FluentValidation.Results
{
	public partial class ValidationFailure
    {
        /// <summary>
        /// Creates a new ValidationFailure with ErrorCode.
        /// </summary>
        public ValidationFailure(string propertyName, string error, object attemptedValue, string errorCode)
            : this(propertyName, error, attemptedValue)
        {
            this.ErrorCode = errorCode;
        }

        public static Func<string, string> ErrorCodeResolver { get; set; } = ServiceStackErrorCodeResolver;

        public static Dictionary<string, string> ErrorCodeAliases = new Dictionary<string, string>
        {
            { "ExactLength", "Length" },
        };

        //ServiceStack uses 'NotNull' instead of FluentValidation 7's 'NotNullValidator' ErrorCode
        public static string ServiceStackErrorCodeResolver(string errorCode)
        {
            var ssCode = errorCode?.Replace("Validator", "");

            return ErrorCodeAliases.TryGetValue(ssCode, out var errorCodeAlias) 
                ? errorCodeAlias
                : ssCode;
        }


        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        string errorCode;
		public string ErrorCode
        {
            get => errorCode;
            set => errorCode = ErrorCodeResolver(value);
        }
	}
}