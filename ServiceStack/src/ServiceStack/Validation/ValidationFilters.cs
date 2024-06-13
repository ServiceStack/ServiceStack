using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Web;

namespace ServiceStack.Validation;

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
        var requestType = requestDto.GetType();
        await Validators.AssertTypeValidatorsAsync(req, requestDto, requestType);

        var validator = ValidatorCache.GetValidator(req, requestType);
        if (validator == null)
            return;

        using (validator as IDisposable)
        {
            if (validator is IHasTypeValidators { TypeValidators.Count: > 0 } hasTypeValidators)
            {
                foreach (var scriptValidator in hasTypeValidators.TypeValidators)
                {
                    await scriptValidator.ThrowIfNotValidAsync(requestDto, req);
                }
            }
                    
            try
            {
                if (req.Verb == HttpMethods.Patch)
                {
                    // Ignore property rules for AutoCrud Patch operations with default values that aren't reset (which are ignored)
                    if (validator is IServiceStackValidator ssValidator && requestDto is ICrud && requestType.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>)))
                    {
                        var typeProperties = TypeProperties.Get(requestType);
                        var propsWithDefaultValues = new HashSet<string>();
                        var resetFields = GetResetFields(req.GetParam(Keywords.reset))?.ToSet(StringComparer.OrdinalIgnoreCase)
                                          ?? TypeConstants<string>.EmptyHashSet;
                            
                        foreach (var entry in typeProperties.PropertyMap)
                        {
                            if (entry.Value.PublicGetter == null || resetFields.Contains(entry.Key))
                                continue;
                            var defaultValue = entry.Value.PropertyInfo.PropertyType.GetDefaultValue();
                            var propValue = entry.Value.PublicGetter(requestDto);
                            if (propValue == null || propValue.Equals(defaultValue))
                                propsWithDefaultValues.Add(entry.Key);
                        }
                        if (propsWithDefaultValues.Count > 0)
                            ssValidator.RemovePropertyRules(rule => propsWithDefaultValues.Contains(rule.PropertyName));
                    }
                }
                    
                var validationResult = await validator.ValidateAsync(req, requestDto);
    
                if (treatInfoAndWarningsAsErrors && validationResult.IsValid)
                    return;
    
                if (!treatInfoAndWarningsAsErrors &&
                    (validationResult.IsValid || validationResult.Errors.All(v => v.Severity != Severity.Error)))
                    return;
    
                var errorResponse =
                    await HostContext.RaiseServiceException(req, requestDto, validationResult.ToException())
                    ?? DtoUtils.CreateErrorResponse(requestDto, validationResult.ToErrorResult());
   
                var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();
                if (autoBatchIndex != null)
                {
                    var responseStatus = errorResponse.GetResponseStatus();
                    if (responseStatus != null)
                    {
                        responseStatus.Meta ??= new Dictionary<string, string>();
                        responseStatus.Meta[Keywords.AutoBatchIndex] = autoBatchIndex;
                    }
                }
                    
                var validationFeature = HostContext.GetPlugin<ValidationFeature>();
                if (validationFeature?.ErrorResponseFilter != null)
                {
                    errorResponse = validationFeature.ErrorResponseFilter(req, validationResult, errorResponse);
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
        }
    }

    public static async Task ResponseFilterAsync(IRequest req, IResponse res, object requestDto)
    {
        if (requestDto is not IHasResponseStatus response)
            return;

        var validator = ValidatorCache.GetValidator(req, req.Dto.GetType());
        if (validator == null)
            return;

        var validationResult = await ValidateAsync(validator, req, req.Dto);

        if (!validationResult.IsValid)
        {
            var responseStatus = response.ResponseStatus
                                 ?? DtoUtils.CreateResponseStatus(validationResult.Errors[0].ErrorCode);
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
    }

    public static async Task<ValidationResult> ValidateAsync(this IValidator validator, IRequest req, object requestDto)
    {
        if (validator == null)
            throw new ArgumentNullException(nameof(validator));
        if (req == null)
            throw new ArgumentNullException(nameof(req));
        if (requestDto == null)
            throw new ArgumentNullException(nameof(requestDto));
            
        var ruleSet = req.Verb;
        using (validator as IDisposable)
        {
            var validationContext = new ValidationContext<object>(requestDto, null, 
                new MultiRuleSetValidatorSelector(ruleSet)) {
                Request = req
            };
                
            if (validator.HasAsyncValidators(validationContext,ruleSet))
            {
                return await validator.ValidateAsync(validationContext);
            }

            // ReSharper disable once MethodHasAsyncOverload
            return validator.Validate(validationContext);
        }
    }

    public static ValidationResult Validate(this IValidator validator, IRequest req, object requestDto)
    {
        if (validator == null)
            throw new ArgumentNullException(nameof(validator));
        if (req == null)
            throw new ArgumentNullException(nameof(req));
        if (requestDto == null)
            throw new ArgumentNullException(nameof(requestDto));
            
        var ruleSet = req.Verb;
        using (validator as IDisposable)
        {
            var validationContext = new ValidationContext<object>(requestDto, null, 
                new MultiRuleSetValidatorSelector(ruleSet)) {
                Request = req
            };
                
            if (validator.HasAsyncValidators(validationContext,ruleSet))
                throw new NotSupportedException($"Use {nameof(ValidateAsync)} to call async validator '{validator.GetType().Name}'");

            return validator.Validate(validationContext);
        }
    }
        
    public static IEnumerable<string> GetResetFields(object o) => o == null
        ? null
        : o is string s
            ? s.Split(',').Map(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
            : o is IEnumerable<string> e
                ? e
                : throw new NotSupportedException($"'{Keywords.Reset}' is not a list of field names");

    public static IEnumerable<string> GetResetFields(this IRequest req) => GetResetFields(req.GetParam(Keywords.reset));
}