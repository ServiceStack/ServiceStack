using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public static class ValidationFilters
    {
        public static void RequestFilter(IRequest req, IResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            var ruleSet = req.Verb;
            if (validator == null || validator.HasAsyncValidators(ruleSet))
                return;
            try
                {
                var validationResult = validator.Validate(
                    new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)) {
                        Request = req
                    });

                if (validationResult.IsValid)
                    return;

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
        
        public static Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            var ruleSet = req.Verb;
            if (validator == null || !validator.HasAsyncValidators(ruleSet))
                return TypeConstants.EmptyTask;

            var validateTask = validator.ValidateAsync(
                new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet))
                {
                    Request = req
                });

            return HostContext.Async.ContinueWith(req, validateTask, t =>
                {
                    if (t.IsFaulted)
                    {
                        var validationEx = t.Exception.UnwrapIfSingleException();
                        
                        var errorResponse = HostContext.RaiseServiceException(req, requestDto, validationEx)
                                            ?? DtoUtils.CreateErrorResponse(requestDto, validationEx);

                        return res.WriteToResponse(req, errorResponse);
                    }
                    else
                    {
                        var validationResult = t.Result;
                        if (validationResult.IsValid)
                            return TypeConstants.TrueTask;

                        var errorResponse = HostContext.RaiseServiceException(req, requestDto, validationResult.ToException())
                                            ?? DtoUtils.CreateErrorResponse(requestDto, validationResult.ToErrorResult());

                        var validationFeature = HostContext.GetPlugin<ValidationFeature>();
                        if (validationFeature?.ErrorResponseFilter != null)
                        {
                            errorResponse = validationFeature.ErrorResponseFilter(validationResult, errorResponse);
                        }

                        return res.WriteToResponse(req, errorResponse);
                    }
                })
                .ContinueWith(t =>
                {
                    using (validator as IDisposable) { }
                });
        }
        
    }
}
