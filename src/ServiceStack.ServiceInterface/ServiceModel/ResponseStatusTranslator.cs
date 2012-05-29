/*
// $Id: ResponseStatusTranslator.cs 12245 2010-02-23 14:55:31Z Demis Bellot $
//
// Revision      : $Revision: 12245 $
// Modified Date : $LastChangedDate: 2010-02-23 14:55:31 +0000 (Tue, 23 Feb 2010) $
// Modified By   : $LastChangedBy: Demis Bellot $
//
// (c) Copyright 2010 Liquidbit Ltd
*/

using System;
using System.Collections.Generic;
using ServiceStack.Common.Extensions;
using ServiceStack.DesignPatterns.Translator;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.Validation;

namespace ServiceStack.ServiceInterface.ServiceModel
{
    /// <summary>
    /// Translates a ValidationResult into a ResponseStatus DTO fragment.
    /// </summary>
    public class ResponseStatusTranslator
        : ITranslator<ResponseStatus, ValidationErrorResult>
    {
        public static readonly ResponseStatusTranslator Instance 
            = new ResponseStatusTranslator();

        public ResponseStatus Parse(Exception exception)
        {
            var validationError = exception as ValidationError;
            if (validationError != null)
            {
                return this.Parse(validationError);
            }

            var validationException = exception as ValidationException;
            if (validationException != null)
            {
                return this.Parse(validationException);
            }

            var httpError = exception as IHttpError;
            return httpError != null
                ? CreateErrorResponse(httpError.ErrorCode, httpError.Message)
                : CreateErrorResponse(exception.GetType().Name, exception.Message);
        }

        public ResponseStatus Parse(ValidationError validationException)
        {
            return CreateErrorResponse(validationException.ErrorCode, validationException.Message, validationException.Violations);
        }

        public ResponseStatus Parse(ValidationException validationException)
        {
            var errors = validationException.Errors.ConvertAll(x => 
                new ValidationErrorField(x.ErrorCode, x.PropertyName, x.ErrorMessage));

            return CreateErrorResponse(typeof(ValidationException).Name, validationException.Message, errors);
        }

        public ResponseStatus Parse(ValidationErrorResult validationResult)
        {
            return validationResult.IsValid
                ? CreateSuccessResponse(validationResult.SuccessMessage)
                : CreateErrorResponse(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
        }

        public static ResponseStatus CreateSuccessResponse(string message)
        {
            return new ResponseStatus { Message = message };
        }

        public static ResponseStatus CreateErrorResponse(string errorCode)
        {
            var errorMessage = errorCode.SplitCamelCase();
            return CreateErrorResponse(errorCode, errorMessage, null);
        }

        public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage)
        {
            return CreateErrorResponse(errorCode, errorMessage, null);
        }

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
        public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage, IEnumerable<ValidationErrorField> validationErrors)
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