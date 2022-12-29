using System;
using System.Collections.Generic;

namespace ServiceStack.Validation
{
    public class ValidationErrorField : IMeta
    {
        public ValidationErrorField(string errorCode, string fieldName) 
            : this(errorCode, fieldName, null) {}

        public ValidationErrorField(string errorCode)
            : this(errorCode, null, null) { }

        public ValidationErrorField(Enum errorCode)
            : this(errorCode.ToString(), null, null) { }

        public ValidationErrorField(Enum errorCode, string fieldName)
            : this(errorCode.ToString(), fieldName, null) { }

        public ValidationErrorField(Enum errorCode, string fieldName, string errorMessage)
            : this(errorCode.ToString(), fieldName, errorMessage) { }

        public ValidationErrorField(string errorCode, string fieldName, string errorMessage)
			: this (errorCode, fieldName, errorMessage, null) { }

		public ValidationErrorField(string errorCode, string fieldName, string errorMessage, object attemptedValue)
        {
            this.ErrorCode = errorCode;
            this.FieldName = fieldName;
            this.ErrorMessage = errorMessage ?? errorCode.ToEnglish();
			this.AttemptedValue = attemptedValue;
        }

        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string FieldName { get; set; }
		public object AttemptedValue { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }
}