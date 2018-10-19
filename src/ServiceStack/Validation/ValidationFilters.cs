using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public static class ValidationFilters
    {
        public static async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
        {
            await RequestFilterAsync(req, res, requestDto, true);
        }

        public static async Task RequestFilterAsyncIgnoreWarningsInfo(IRequest req, IResponse res, object requestDto)
        {
            await RequestFilterAsync(req, res, requestDto, false);
        }

        private static async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto,
            bool treatInfoAndWarningsAsErrors)
        {
            var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
            if (validator == null)
                return;

            try
            {
                var validationResult = await Validate(validator, req, requestDto);

                if (treatInfoAndWarningsAsErrors && validationResult.IsValid)
                {
                    return;
                }

                if (!treatInfoAndWarningsAsErrors &&
                    (validationResult.IsValid || validationResult.Errors.All(v => v.Severity != Severity.Error)))
                {
                    return;
                }

                var errorResponse =
                    await HostContext.RaiseServiceException(req, requestDto, validationResult.ToException())
                    ?? DtoUtils.CreateErrorResponse(requestDto, validationResult.ToErrorResult());

                var autoBatchIndex = req.GetItem("AutoBatchIndex")?.ToString();
                if (autoBatchIndex != null)
                {
                    var responseStatus = errorResponse.GetResponseStatus();
                    if (responseStatus != null)
                    {
                        if (responseStatus.Meta == null)
                            responseStatus.Meta = new Dictionary<string, string>();

                        responseStatus.Meta["AutoBatchIndex"] = autoBatchIndex;
                    }
                }

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
                using (validator as IDisposable)
                {
                }
            }
        }

        public static async Task ResponseFilterAsync(IRequest req, IResponse res, object requestDto)
        {
            var response = requestDto as IHasResponseStatus;
            if (response == null)
                return;

            var validator = ValidatorCache.GetValidator(req, req.Dto.GetType());
            if (validator == null)
                return;

            var validationResult = await Validate(validator, req, req.Dto);

            if (validationResult.IsValid) 
                return;
            
            var responseStatus = response.ResponseStatus ??
                                 DtoUtils.CreateResponseStatus(validationResult.Errors[0].ErrorCode);
            foreach (var error in validationResult.Errors)
            {
                var responseError = new ResponseError
                {
                    ErrorCode = error.ErrorCode,
                    FieldName = error.PropertyName,
                    Message = error.ErrorMessage,
                    Meta = new Dictionary<string, string> {["Severity"] = error.Severity.ToString()}
                };
                responseStatus.Errors.Add(responseError);
            }

            response.ResponseStatus = responseStatus;
        }

        private static async Task<ValidationResult> Validate(IValidator validator, IRequest req, object requestDto)
        {
            var ruleSet = req.Verb;

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

            using (validator as IDisposable) { }
            
            return validationResult;
        }
    }
}