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
            var validationResult = result.ToErrorResult();
            throw new ValidationError(validationResult);
        }

        public static ValidationErrorResult ToErrorResult(this ValidationResult result)
        {
            var validationResult = new ValidationErrorResult();
            foreach (var error in result.Errors)
                validationResult.Errors.Add(new ValidationErrorField(error.ErrorCode, error.PropertyName, error.ErrorMessage));

            return validationResult;
        }
    }
}
