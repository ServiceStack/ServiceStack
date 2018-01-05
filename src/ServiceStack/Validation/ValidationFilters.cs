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
        public static async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            var ruleSet = req.Verb;
            if (validator == null)
                return;

            try
            {
                ValidationResult validationResult;

                if (validator.HasAsyncValidators(ruleSet))
                {
                    validationResult = await validator.ValidateAsync(
                        new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet))
                        {
                            Request = req
                        });
                }
                else
                {
                    validationResult = validator.Validate(
                        new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet))
                        {
                            Request = req
                        });
                }

                if (validationResult.IsValid)
                    return;

                var errorResponse = await HostContext.RaiseServiceException(req, requestDto, validationResult.ToException())
                                    ?? DtoUtils.CreateErrorResponse(requestDto, validationResult.ToErrorResult());

                var validationFeature = HostContext.GetPlugin<ValidationFeature>();
                if (validationFeature?.ErrorResponseFilter != null)
                {
                    errorResponse = validationFeature.ErrorResponseFilter(validationResult, errorResponse);
                }

                await res.WriteToResponse(req, errorResponse);
            }
            catch (Exception ex)
            {
                var validationEx = ex.UnwrapIfSingleException();

                var errorResponse = await HostContext.RaiseServiceException(req, requestDto, validationEx)
                                    ?? DtoUtils.CreateErrorResponse(requestDto, validationEx);

                await res.WriteToResponse(req, errorResponse);
            }
            finally
            {
                using (validator as IDisposable) { }
            }
        }
        
    }
}
