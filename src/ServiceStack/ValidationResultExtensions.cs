using System;
using System.Collections.Generic;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Validation;

namespace ServiceStack
{
    using ServiceStack.Text;

    public static class ValidationResultExtensions
    {
        internal static Dictionary<string, string> CustomStateAsDictionary(this ValidationFailure error)
        {
            try
            {
                if (error.CustomState != null)
                {
                    if (error.CustomState is IEnumerable<KeyValuePair<string,string>>)
                        return error.CustomState.ToStringDictionary();
                    return error.CustomState.ToObjectDictionary().ToStringDictionary();
                }
                return error.FormattedMessagePlaceholderValues.ToStringDictionary();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts the validation result to an error result which will be serialized by ServiceStack in a clean and human-readable way.
        /// </summary>
        /// <param name="result">The validation result</param>
        /// <returns></returns>
        public static ValidationErrorResult ToErrorResult(this ValidationResult result)
        {
            var validationResult = new ValidationErrorResult();
            foreach (var error in result.Errors)
            {
                validationResult.Errors.Add(new ValidationErrorField(error.ErrorCode, error.PropertyName, error.ErrorMessage, error.AttemptedValue)
                {
                    Meta = error.CustomStateAsDictionary()
                });
            }

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
    }
}

