using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Validation;
using FluentValidation.Results;

namespace ServiceStack.ServiceInterface.Validation
{
    public static class ValidationResultExtensions
    {
        public static void Throw(this ValidationResult result)
        {
            SerializableValidationResult validationResult = new SerializableValidationResult();
            foreach (var error in result.Errors)
                validationResult.Errors.Add(new SerializableValidationError(error.ErrorMessage, error.PropertyName));

            throw new SerializableValidationException(validationResult);
        }
    }
}
