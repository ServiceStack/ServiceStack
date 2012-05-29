using ServiceStack.ServiceHost;
using ServiceStack.FluentValidation;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Validation
{
    public class ValidationFilters
    {
        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            if (validator != null)
            {
                var validatorWithHttpRequest = validator as IRequiresHttpRequest;
                if (validatorWithHttpRequest != null)
                    validatorWithHttpRequest.HttpRequest = req;

                string ruleSet = req.HttpMethod;
                var validationResult = validator.Validate(
                    new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)));

                if (validationResult.IsValid) return;

                var errorResponse = ServiceUtils.CreateErrorResponse(
                    requestDto, validationResult.ToErrorResult());

                res.WriteToResponse(req, errorResponse);
            }
        }		
    }
}
