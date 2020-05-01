namespace ServiceStack
{
    public static class ValidatorUtils
    {
        public static ITypeValidator Init(this ITypeValidator validator, IValidateRule rule)
        {
            if (rule.ErrorCode != null)
                validator.ErrorCode = rule.ErrorCode;
            if (rule.Message != null)
                validator.Message = rule.Message;
            if (rule is ValidateRequestAttribute attr)
            {
                if (attr.StatusCode != default)
                    validator.StatusCode = attr.StatusCode;
            }

            return validator;
        }

        internal static string RemoveValidatorSuffix(this string name) => 
            StringUtils.RemoveSuffix(name, "Validator");
    }
}