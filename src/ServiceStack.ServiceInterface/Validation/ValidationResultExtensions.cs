using System.Globalization;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Html;
using ServiceStack.Validation;

namespace ServiceStack.ServiceInterface.Validation
{
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// Converts the validation result to an error result which will be serialized by ServiceStack in a clean and human-readable way.
        /// </summary>
        /// <param name="result">The validation result</param>
        /// <returns></returns>
        public static ValidationErrorResult ToErrorResult(this ValidationResult result)
        {
            var validationResult = new ValidationErrorResult();
            foreach (var error in result.Errors)
                validationResult.Errors.Add(new ValidationErrorField(error.ErrorCode, error.PropertyName, error.ErrorMessage));

            return validationResult;
        }

        /// <summary>
        /// Converts the validation result to an error exception which will be serialized by ServiceStack in a clean and human-readable way
        /// if the returned exception is thrown.
        /// </summary>
        /// <param name="result">The validation result</param>
        /// <returns></returns>
        public static ValidationError ToException(this ValidationResult result)
        {
            return new ValidationError(result.ToErrorResult());
        }

        /// <summary>
        /// Stores the errors in a ValidationResult object to the specified modelstate dictionary.
        /// </summary>
        /// <param name="result">The validation result to store</param>
        /// <param name="modelState">The ModelStateDictionary to store the errors in.</param>
        /// <param name="prefix">An optional prefix. If ommitted, the property names will be the keys. If specified, the prefix will be concatenatd to the property name with a period. Eg "user.Name"</param>
        public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState, string prefix)
        {
            if (!result.IsValid) {
                foreach (var error in result.Errors) {
                    string key = string.IsNullOrEmpty(prefix) ? error.PropertyName : prefix + "." + error.PropertyName;
                    modelState.AddModelError(key, error.ErrorMessage);
                    //To work around an issue with MVC: SetModelValue must be called if AddModelError is called.
                    modelState.SetModelValue(key, new ValueProviderResult(error.AttemptedValue ?? "", (error.AttemptedValue ?? "").ToString(), CultureInfo.CurrentCulture));
                }
            }
        }
    }
}

