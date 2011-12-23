using System;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Validation
{
	public class SerializableValidationError
	{
		public SerializableValidationError(string errorCode, string fieldName) 
			: this(errorCode, fieldName, null) {}

		public SerializableValidationError(string errorCode)
			: this(errorCode, null, null) { }

		public SerializableValidationError(Enum errorCode)
			: this(errorCode.ToString(), null, null) { }

		public SerializableValidationError(Enum errorCode, string fieldName)
			: this(errorCode.ToString(), fieldName, null) { }

		public SerializableValidationError(Enum errorCode, string fieldName, string errorMessage)
			: this(errorCode.ToString(), fieldName, errorMessage) { }

		public SerializableValidationError(string errorCode, string fieldName, string errorMessage)
		{
			this.ErrorCode = errorCode;
			this.FieldName = fieldName;
			this.ErrorMessage = errorMessage ?? errorCode.ToEnglish();
		}

		public string ErrorCode { get; set; }
		public string ErrorMessage { get; set; }
		public string FieldName { get; set; }
	}
}