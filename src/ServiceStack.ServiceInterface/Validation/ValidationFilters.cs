using ServiceStack.ServiceHost;
using ServiceStack.FluentValidation;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Validation
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

            res.WriteToResponse(req, errorResponse);
        }
    }
}
