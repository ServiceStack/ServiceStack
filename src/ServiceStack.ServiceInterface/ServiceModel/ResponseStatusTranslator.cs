/*
// $Id: ResponseStatusTranslator.cs 12245 2010-02-23 14:55:31Z Demis Bellot $
//
// Revision      : $Revision: 12245 $
// Modified Date : $LastChangedDate: 2010-02-23 14:55:31 +0000 (Tue, 23 Feb 2010) $
// Modified By   : $LastChangedBy: Demis Bellot $
//
// (c) Copyright 2012 ServiceStack
*/

using System;
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
                ? DtoUtils.CreateErrorResponse(httpError.ErrorCode, httpError.Message)
                : DtoUtils.CreateErrorResponse(exception.GetType().Name, exception.Message);
        }

        public ResponseStatus Parse(ValidationError validationException)
        {
            return DtoUtils.CreateErrorResponse(validationException.ErrorCode, validationException.Message, validationException.Violations);
        }

        public ResponseStatus Parse(ValidationException validationException)
        {
            var errors = validationException.Errors.ConvertAll(x => 
                new ValidationErrorField(x.ErrorCode, x.PropertyName, x.ErrorMessage));

            return DtoUtils.CreateErrorResponse(typeof(ValidationException).Name, validationException.Message, errors);
        }

        public ResponseStatus Parse(ValidationErrorResult validationResult)
        {
            return validationResult.IsValid
                ? DtoUtils.CreateSuccessResponse(validationResult.SuccessMessage)
                : DtoUtils.CreateErrorResponse(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
        }
    }
}