using System;
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

            try
            {
                var ruleSet = req.Verb;
                var validationResult = validator.Validate(
                    new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)) {
                        Request = req
                    });

                if (validationResult.IsValid) return;

                var errorResponse = HostContext.RaiseServiceException(req, requestDto, validationResult.ToException())
                    ?? DtoUtils.CreateErrorResponse(requestDto, validationResult.ToErrorResult());

                var validationFeature = HostContext.GetPlugin<ValidationFeature>();
                if (validationFeature?.ErrorResponseFilter != null)
                {
                    errorResponse = validationFeature.ErrorResponseFilter(validationResult, errorResponse);
                }

                res.WriteToResponse(req, errorResponse);
            }
            finally
            {
                using (validator as IDisposable) {}
            }
        }
    }
}
