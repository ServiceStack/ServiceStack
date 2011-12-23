using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Validation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceInterface.Validation
{
    public static class ValidationResultExtensions
    {
        public static void Throw(this ValidationResult result)
        {
            var validationResult = result.AsSerializable();
            throw new SerializableValidationException(validationResult);
        }

        public static SerializableValidationResult AsSerializable(this ValidationResult result)
        {
            var validationResult = new SerializableValidationResult();
            foreach (var error in result.Errors)
                validationResult.Errors.Add(new SerializableValidationError(error.ErrorMessage, error.PropertyName));

            return validationResult;
        }
    }
}
