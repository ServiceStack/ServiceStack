using ServiceStack.FluentValidation;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public static class ValidationFilters
    {
        public static void RequestFilter(IRequest req, IResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            if (validator == null) return;

            var validatorWithHttpRequest = validator as IRequiresRequest;
            if (validatorWithHttpRequest != null)
                validatorWithHttpRequest.Request = req;

            var ruleSet = req.Verb;
            var validationResult = validator.Validate(
                new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)));

            if (validationResult.IsValid) return;

            HostContext.RaiseServiceException(req, requestDto, validationResult.ToException());

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
