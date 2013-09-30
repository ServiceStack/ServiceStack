using ServiceStack.FluentValidation;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public static class ValidationFilters
    {
        public static void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            if (validator == null) return;

            var validatorWithHttpRequest = validator as IRequiresHttpRequest;
            if (validatorWithHttpRequest != null)
                validatorWithHttpRequest.HttpRequest = req;

            var ruleSet = req.HttpMethod;
            var validationResult = validator.Validate(
                new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)));

            if (validationResult.IsValid) return;

            var errorResponse = DtoUtils.CreateErrorResponse(
                requestDto, validationResult.ToErrorResult());

            var validationFeature = HostContext.GetPlugin<ValidationFeature>();
            if (validationFeature != null && validationFeature.ErrorResponseFilter != null)
            {
                errorResponse = validationFeature.ErrorResponseFilter(validationResult, errorResponse);
            }

            res.WriteToResponse(req, errorResponse);
        }
    }
}
