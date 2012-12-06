using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Validation;

namespace ServiceStack.Common.Utils
{
    public static class ResponseStatusUtils
    {
        /// <summary>
        /// Creates the error response from the values provided.
        /// 
        /// If the errorCode is empty it will use the first validation error code, 
        /// if there is none it will throw an error.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="validationErrors">The validation errors.</param>
        /// <returns></returns>
        public static ResponseStatus CreateResponseStatus(string errorCode, string errorMessage, IEnumerable<ValidationErrorField> validationErrors)
        {
            var to = new ResponseStatus {
                ErrorCode = errorCode,
                Message = errorMessage,
                Errors = new List<ResponseError>(),
            };
            if (validationErrors != null)
            {
                foreach (var validationError in validationErrors)
                {
                    var error = new ResponseError {
                        ErrorCode = validationError.ErrorCode,
                        FieldName = validationError.FieldName,
                        Message = validationError.ErrorMessage,
                    };
                    to.Errors.Add(error);

                    if (string.IsNullOrEmpty(to.ErrorCode))
                    {
                        to.ErrorCode = validationError.ErrorCode;
                    }
                    if (string.IsNullOrEmpty(to.Message))
                    {
                        to.Message = validationError.ErrorMessage;
                    }
                }
            }
            if (string.IsNullOrEmpty(errorCode))
            {
                if (string.IsNullOrEmpty(to.ErrorCode))
                {
                    throw new ArgumentException("Cannot create a valid error response with a en empty errorCode and an empty validationError list");
                }
            }
            return to;
        }
    }
}