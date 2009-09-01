using System;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation
{
	public class ValidationError
	{
		public ValidationError(string errorCode, string fieldName) 
			: this(errorCode, fieldName, null) {}

		public ValidationError(string errorCode)
			: this(errorCode, null, null) { }

		public ValidationError(Enum errorCode)
			: this(errorCode.ToString(), null, errorCode.ToString().SplitCamelCase()) { }

		public ValidationError(Enum errorCode, string fieldName)
			: this(errorCode.ToString(), fieldName, errorCode.ToString().SplitCamelCase()) { }

		public ValidationError(Enum errorCode, string fieldName, string errorMessage)
			: this(errorCode.ToString(), fieldName, errorMessage) { }

		public ValidationError(string errorCode, string fieldName, string errorMessage)
		{
			this.ErrorCode = errorCode;
			this.FieldName = fieldName;
			this.ErrorMessage = errorMessage ?? errorCode.SplitCamelCase();
		}

		public string ErrorCode { get; set; }
		public string ErrorMessage { get; set; }
		public string FieldName { get; set; }
	}
}