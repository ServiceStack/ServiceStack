using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Host;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Validation;

public class ValidationFeature : IPlugin, IPostConfigureServices, IPreInitPlugin, IAfterInitAppHost, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Validation;
    public int Priority => ConfigurePriority.ValidationFeature;

    public Func<IRequest, ValidationResult, object, object> ErrorResponseFilter { get; set; }

    public bool ScanAppHostAssemblies { get; set; } = true;
    public bool TreatInfoAndWarningsAsErrors { get; set; } = true;
    public bool EnableDeclarativeValidation { get; set; } = true;

    public bool ImplicitlyValidateChildProperties { get; set; } = true;
    public string AccessRole { get; set; } = RoleNames.Admin;
        
    public IValidationSource ValidationSource { get; set; } 
        
    public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
        [typeof(GetValidationRulesService)] = ["/" + "validation/rules".Localize() + "/{Type}"],
        [typeof(ModifyValidationRulesService)] = ["/" + "validation/rules".Localize()],
    };

    /// <summary>
    /// Specify default ErrorCodes to use when custom validation conditions are invalid
    /// </summary>
    public Dictionary<string, string> ConditionErrorCodes => Validators.ConditionErrorCodes;

    /// <summary>
    /// Specify default Error Messages to use when Validators with these ErrorCode's are invalid
    /// </summary>
    public Dictionary<string, string> ErrorCodeMessages => Validators.ErrorCodeMessages;
        
    public void BeforePluginsLoaded(IAppHost appHost)
    {
        if (appHost.TryResolve<IValidationSource>() is IValidationSourceAdmin)
        {
            appHost.ConfigurePlugin<UiFeature>(feature => {
                feature.AddAdminLink(AdminUiFeature.Validation, new LinkInfo {
                    Id = "validation",
                    Label = "Validation",
                    Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Lock)),
                    Show = AccessRole != null ? $"role:{AccessRole}" : null,
                });
            });
        }
    }

    public void AfterConfigure(IServiceCollection services)
    {
        if (ValidationSource != null)
        {
            services.AddSingleton(ValidationSource);
        }
        
        var hasValidationSource = services.Exists<IValidationSource>();
        if (hasValidationSource && AccessRole != null)
        {
            services.RegisterServices(ServiceRoutes);
        }

        if (ScanAppHostAssemblies)
        {
            var assemblies = ServiceStackHost.InitOptions.ResolveAllServiceAssemblies().ToArray();
            services.RegisterValidators(assemblies);
        }
        
        if (EnableDeclarativeValidation)
        {
            string[] autoQueryPlugins = [Plugins.AutoQuery, Plugins.AutoQueryData];
            Func<Type,bool> include = ServiceStackHost.InitOptions.Plugins.OfType<Model.IHasStringId>()
                .Any(plugin => autoQueryPlugins.Contains(plugin.Id))
                ? Crud.AnyAutoQueryType
                : null;

            var requestTypes = ServiceStackHost.InitOptions.ResolveAssemblyRequestTypes(include);
            foreach (var requestType in requestTypes)
            {
                var hasValidateRequestAttrs = Validators.HasValidateRequestAttributes(requestType);
                if (hasValidateRequestAttrs)
                {
                    Validators.RegisterRequestRulesFor(requestType);
                }
                        
                var hasValidateAttrs = Validators.HasValidateAttributes(requestType);
                if (hasValidationSource || hasValidateAttrs)
                {
                    services.RegisterNewValidatorIfNotExists(requestType, ImplicitlyValidateChildProperties);
                    Validators.RegisterPropertyRulesFor(services, requestType, ImplicitlyValidateChildProperties);
                }
            }
        }
        
        var existingDtoValidators = ValidationExtensions.RegisteredDtoValidators.CreateCopy();

        foreach (var dtoType in existingDtoValidators)
        {
            Validators.RegisterPropertyRulesFor(services, dtoType, ImplicitlyValidateChildProperties);
        }
    }
    
    /// <summary>
    /// Activate the validation mechanism, so every request DTO with an existing validator
    /// will be validated.
    /// </summary>
    /// <param name="appHost">The app host</param>
    public void Register(IAppHost appHost)
    {
        if (TreatInfoAndWarningsAsErrors)
        {
            if (!appHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
            {
                appHost.GlobalRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
            }

            if (!appHost.GlobalMessageRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
            {
                appHost.GlobalMessageRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
            }
        }
        else
        {
            if (!appHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo))
            {
                appHost.GlobalRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo);
            }

            if (!appHost.GlobalMessageRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo))
            {
                appHost.GlobalMessageRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo);
            }
                
            if (!appHost.GlobalResponseFiltersAsync.Contains(ValidationFilters.ResponseFilterAsync))
            {
                appHost.GlobalResponseFiltersAsync.Add(ValidationFilters.ResponseFilterAsync);
            }

            if (!appHost.GlobalMessageResponseFiltersAsync.Contains(ValidationFilters.ResponseFilterAsync))
            {
                appHost.GlobalMessageResponseFiltersAsync.Add(ValidationFilters.ResponseFilterAsync);
            }

            if (!appHost.GatewayResponseFiltersAsync.Contains(ValidationFilters.GatewayResponseFiltersAsync))
            {
                appHost.GatewayResponseFiltersAsync.Add(ValidationFilters.GatewayResponseFiltersAsync);
            }
        }

        ValidationSource ??= appHost.TryResolve<IValidationSource>();
        ValidationSource?.InitSchema();

        var hasValidationSource = ValidationSource != null; 
        var hasValidationSourceAdmin = ValidationSource is IValidationSourceAdmin;

        appHost.AddToAppMetadata(metadata =>
        {
            metadata.Plugins.Validation = new ValidationInfo {
                HasValidationSource = hasValidationSource.NullIfFalse(), 
                HasValidationSourceAdmin = hasValidationSourceAdmin.NullIfFalse(),
                ServiceRoutes = ServiceRoutes.ToMetadataServiceRoutes(),
                TypeValidators = !EnableDeclarativeValidation ? null
                    : appHost.ScriptContext.ScriptMethods.SelectMany(x => 
                            ScriptMethodInfo.GetScriptMethods(x.GetType(), where:mi => 
                                typeof(ITypeValidator).IsAssignableFrom(mi.ReturnType)))
                        .Map(x => x.ToScriptMethodType()),
                PropertyValidators = !EnableDeclarativeValidation ? null
                    : appHost.ScriptContext.ScriptMethods.SelectMany(x => 
                            ScriptMethodInfo.GetScriptMethods(x.GetType(), where:mi => 
                                typeof(IPropertyValidator).IsAssignableFrom(mi.ReturnType)))
                        .Map(x => x.ToScriptMethodType()),
                AccessRole = AccessRole,
            };
        });

        appHost.PostConfigurePlugin<MetadataFeature>(c =>
        {
            c.ExportTypes.Add(typeof(ValidationRule));
        });
    }

    public void AfterInit(IAppHost appHost)
    {
        if (!EnableDeclarativeValidation) return;
        
        Validators.ConfigureDelayedPropertyRules();

        foreach (var op in appHost.Metadata.Operations)
        {
            op.AddRequestTypeValidationRules(Validators.GetTypeRules(op.RequestType));
            op.AddRequestPropertyValidationRules(Validators.GetPropertyRules(op.RequestType));
        }
    }

    /// <summary>
    /// Override to provide additional/less context about the Service Exception. 
    /// By default the request is serialized and appended to the ResponseStatus StackTrace.
    /// </summary>
    public virtual string GetRequestErrorBody(object request)
    {
        var requestString = "";
        try
        {
            requestString = TypeSerializer.SerializeToString(request);
        }
        catch /*(Exception ignoreSerializationException)*/
        {
            //Serializing request successfully is not critical and only provides added error info
        }

        return $"[{GetType().GetOperationName()}: {DateTime.UtcNow}]:\n[REQUEST: {requestString}]";
    }

    public virtual void ValidateRequest(object requestDto, IRequest req)
    {
        var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
        if (validator == null) return;
            
        var ruleSet = (string) (req.GetItem(Keywords.InvokeVerb) ?? req.Verb);
        var validationContext = new ValidationContext<object>(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)) {
            Request = req
        };
                    
        var result = validator.Validate(validationContext);
        if (!result.IsValid)
            throw result.ToWebServiceException(requestDto, this);
    }

    public virtual async Task ValidateRequestAsync(object requestDto, IRequest req, CancellationToken token=default)
    {
        var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
        if (validator == null) return;
            
        var ruleSet = (string) (req.GetItem(Keywords.InvokeVerb) ?? req.Verb);
        var validationContext = new ValidationContext<object>(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)) {
            Request = req
        };

        var result = validator.HasAsyncValidators(validationContext)
            ? await validator.ValidateAsync(validationContext, token)
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            : validator.Validate(validationContext);
        
        if (TreatInfoAndWarningsAsErrors && result.IsValid)
        {
            return;
        }
        
        if (!result.IsValid && result.Errors.Any(x => x.Severity == Severity.Error))
        {
            throw result.ToWebServiceException(requestDto, this);
        }
            
        var ver = new ValidationErrorResult();
        foreach (var error in result.Errors)
        {
            var vfe = new ValidationErrorField(error.ErrorCode, error.PropertyName, error.ErrorMessage, error.AttemptedValue);
            if (error.CustomState is IEnumerable<KeyValuePair<string, string>>)
            {
                vfe.Meta = error.CustomState.ToStringDictionary();
            }
            else
            {
                vfe.Meta = error.FormattedMessagePlaceholderValues.ToStringDictionary();
            }

            vfe.Meta[nameof(error.Severity)] = error.Severity.ToString();
            ver.Errors.Add(vfe);
        }

        var errorResponse = DtoUtils.CreateErrorResponse(requestDto, ver);
        req.Items[Keywords.ServiceGatewayResponseStatus] = errorResponse.GetResponseStatus();
    }

    public async Task<ValidationFeature> AssertRequiredRole(IRequest request, string authSecret=null)
    {
        await RequestUtils.AssertAccessRoleAsync(request, accessRole: AccessRole, authSecret: authSecret).ConfigAwait();
        return this;
    }
}

[DefaultRequest(typeof(GetValidationRules))]
[Restrict(VisibilityTo = RequestAttributes.Localhost)]
public class GetValidationRulesService(IValidationSource validationSource) : Service
{
    public async Task<object> Any(GetValidationRules request)
    {
        var feature = await HostContext.AssertPlugin<ValidationFeature>().AssertRequiredRole(Request, request.AuthSecret).ConfigAwait();

        var type = HostContext.Metadata.FindDtoType(request.Type);
        if (type == null)
            throw HttpError.NotFound(request.Type);
            
        return new GetValidationRulesResponse {
            Results = await validationSource.GetAllValidateRulesAsync(request.Type).ConfigAwait(),
        };
    }
}

[DefaultRequest(typeof(ModifyValidationRules))]
[Restrict(VisibilityTo = RequestAttributes.Localhost)]
public class ModifyValidationRulesService(IValidationSource validationSource) : Service
{
    public async Task Any(ModifyValidationRules request)
    {
        var appHost = HostContext.AssertAppHost();
        var feature = await HostContext.AssertPlugin<ValidationFeature>().AssertRequiredRole(Request, request.AuthSecret).ConfigAwait();

        var utcNow = DateTime.UtcNow;
        var userName = (await base.GetSessionAsync().ConfigAwait()).GetUserAuthName();
        var rules = request.SaveRules;

        if (!rules.IsEmpty())
        {
            foreach (var rule in rules)
            {
                if (rule.Type == null)
                    throw new ArgumentNullException(nameof(rule.Type));

                var existingType = appHost.Metadata.FindDtoType(rule.Type);
                if (existingType == null)
                    throw new ArgumentException(@$"{rule.Type} does not exist", nameof(rule.Type));

                if (rule.Validator == "")
                    rule.Validator = null;
                if (rule.Condition == "")
                    rule.Condition = null;
                if (rule.Field == "")
                    rule.Field = null;
                if (rule.ErrorCode == "")
                    rule.ErrorCode = null;
                if (rule.Message == "")
                    rule.Message = null;
                if (rule.Notes == "")
                    rule.Notes = null;
                    
                if (rule.Field != null && TypeProperties.Get(existingType).GetAccessor(rule.Field) == null)
                    throw new ArgumentException(@$"{rule.Field} does not exist on {rule.Type}", nameof(rule.Field));

                if (rule.Validator != null)
                {
                    object validator;
                    try
                    {
                        validator = appHost.EvalExpression(rule.Validator);
                        if (validator == null)
                            throw new ArgumentException(@$"Validator does not exist", nameof(rule.Validator));
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(@$"Invalid Validator: " + e.Message, nameof(rule.Validator));
                    }

                    var validators = (validator as List<object>) ?? TypeConstants.EmptyObjectList;
                    var firstValidator = validator is IPropertyValidator pv
                        ? pv
                        : validator is ITypeValidator tv
                            ? tv
                            : validators?.FirstOrDefault() ?? validator;

                    if (rule.Field != null && !(firstValidator is IPropertyValidator && validators.All(v => v is IPropertyValidator)))
                        throw new ArgumentException(@$"{nameof(IPropertyValidator)} is expected but was {(validators?.FirstOrDefault(v => !(v is IPropertyValidator)) ?? firstValidator).GetType().Name}", nameof(rule.Validator));
                        
                    if (rule.Field == null && !(firstValidator is ITypeValidator && validators.All(v => v is ITypeValidator)))
                        throw new ArgumentException(@$"{nameof(ITypeValidator)} is expected but was {(validators?.FirstOrDefault(v => !(v is IPropertyValidator)) ?? firstValidator).GetType().Name}", nameof(rule.Validator));

                    if (rule.Condition != null)
                        throw new ArgumentException(@$"Only {nameof(rule.Validator)} or {nameof(rule.Condition)} can be specified, not both", nameof(rule.Condition));
                }
                else
                {
                    if (rule.Condition == null)
                        throw new ArgumentNullException(nameof(rule.Validator), @$"{nameof(rule.Validator)} or {nameof(rule.Condition)} is required");

                    try
                    {
                        var ast = Validators.ParseCondition(appHost.ScriptContext, rule.Condition);
                        await ast.Init().ConfigAwait();
                    }
                    catch (Exception e)
                    {
                        var useEx = e is ScriptException se ? se.InnerException ?? e : e;
                        throw new ArgumentException(useEx.Message, nameof(rule.Condition));
                    }
                }

                if (rule.CreatedBy == null)
                {
                    rule.CreatedBy = userName;
                    rule.CreatedDate = utcNow;
                }
                rule.ModifiedBy = userName;
                rule.ModifiedDate = utcNow;
            }

            await validationSource.SaveValidationRulesAsync(rules).ConfigAwait();
        }

        if (!request.SuspendRuleIds.IsEmpty())
        {
            var suspendRules = await validationSource.GetValidateRulesByIdsAsync(request.SuspendRuleIds).ConfigAwait();
            foreach (var suspendRule in suspendRules)
            {
                suspendRule.SuspendedBy = userName;
                suspendRule.SuspendedDate = utcNow;
            }

            await validationSource.SaveValidationRulesAsync(suspendRules).ConfigAwait();
        }

        if (!request.UnsuspendRuleIds.IsEmpty())
        {
            var unsuspendRules = await validationSource.GetValidateRulesByIdsAsync(request.UnsuspendRuleIds).ConfigAwait();
            foreach (var unsuspendRule in unsuspendRules)
            {
                unsuspendRule.SuspendedBy = null;
                unsuspendRule.SuspendedDate = null;
            }

            await validationSource.SaveValidationRulesAsync(unsuspendRules).ConfigAwait();
        }

        if (!request.DeleteRuleIds.IsEmpty())
        {
            await validationSource.DeleteValidationRulesAsync(request.DeleteRuleIds.ToArray()).ConfigAwait();
        }

        if (request.ClearCache.GetValueOrDefault())
        {
            await validationSource.ClearCacheAsync().ConfigAwait();
        }
    }
}

public static class ValidationExtensions
{
    public static HashSet<Type> RegisteredDtoValidators { get; private set; } = new();
    internal static Dictionary<Type,Type> TypesValidatorsMap { get; private set; } = new();
    internal static List<Type> ValidatorTypes { get; private set; } = new();
    private static List<Assembly> RegisteredAssemblies { get; set; } = new();
    private static List<Type> RegisteredValidators { get; set; } = new();

    internal static void Reset()
    {
        RegisteredDtoValidators = new();
        TypesValidatorsMap = new();
        ValidatorTypes = [];
        RegisteredAssemblies = [];
        RegisteredValidators = [];
    }
        
    public static void Init(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            if (!RegisteredAssemblies.Contains(assembly))
            {
                RegisteredAssemblies.Add(assembly);
                foreach (var type in assembly.GetTypes())
                {
                    var genericValidator = type.GetTypeWithGenericInterfaceOf(typeof(IValidator<>));
                    if (genericValidator != null && !type.IsAbstract && !type.IsGenericTypeDefinition)
                    {
                        if (type.GetCustomAttributes<IgnoreServicesAttribute>().Any())
                            continue;
                        ValidatorTypes.Add(type);
                    }
                }
            }
        }
    }
        
    /// <summary>
    /// Auto-scans the provided assemblies for a <see cref="IValidator"/>
    /// and registers it in the provided IoC container.
    /// </summary>
    /// <param name="services">The IoC container</param>
    /// <param name="assemblies">The assemblies to scan for a validator</param>
    public static void RegisterValidators(this IServiceCollection services, params Assembly[] assemblies)
    {
        RegisterValidators(services, ReuseScope.None, assemblies);
    }

    public static void RegisterValidators(this IServiceCollection services, ReuseScope scope, params Assembly[] assemblies)
    {
        Init(assemblies);
        var validatorTypesSnapshot = ValidatorTypes.ToArray();
        var lifetime = scope.ToServiceLifetime();
        foreach (var validatorType in validatorTypesSnapshot)
        {
            services.RegisterValidator(validatorType, lifetime);
        }
    }

    public static void RegisterValidator(this IServiceCollection services, Type validator, ReuseScope scope) =>
        services.RegisterValidator(validator, scope.ToServiceLifetime());
    
    public static void RegisterValidators(this IServiceCollection services, ServiceLifetime lifetime, params Assembly[] assemblies)
    {
        Init(assemblies);
        var validatorTypesSnapshot = ValidatorTypes.ToArray();
        foreach (var validatorType in validatorTypesSnapshot)
        {
            services.RegisterValidator(validatorType, lifetime);
        }
    }

    public static void RegisterValidator<T>(this IServiceCollection services, Func<IServiceProvider,IValidator<T>> factoryFn, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var validatorType = typeof(IValidator<T>);
        if (RegisteredValidators.Contains(validatorType))
            return;
        
        var dtoType = typeof(T);
        ValidatorTypes.AddIfNotExists(validatorType);
        TypesValidatorsMap[dtoType] = validatorType;

        services.Add(typeof(IValidator<T>), factoryFn, lifetime);

        RegisteredDtoValidators.Add(dtoType);
    }
    
    public static void RegisterValidator(this IServiceCollection services, Type validator, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (RegisteredValidators.Contains(validator))
            return;

        var baseType = validator.BaseType;
        if (validator.IsInterface || baseType == null)
            return;

        while (baseType is { IsGenericType: false })
        {
            baseType = baseType.BaseType;
        }

        if (baseType == null)
            return;

        RegisteredValidators.Add(validator);

        var dtoType = baseType.GetGenericArguments()[0];
            
        var validatorType = typeof(IValidator<>).MakeGenericType(dtoType);
        ValidatorTypes.AddIfNotExists(validator);
        TypesValidatorsMap[dtoType] = validator;

        if (!services.Exists(validatorType))
        {
            services.Add(validatorType, validator, lifetime);
        }
        
        RegisteredDtoValidators.Add(dtoType);
    }

    internal static void RegisterNewValidatorIfNotExists(this IServiceCollection services, Type requestType, bool registerChildValidators)
    {
        // We only need to register a new a Validator if it doesn't already exist for the Type 
        if (!RegisteredDtoValidators.Contains(requestType))
        {
            var typeValidator = typeof(DefaultValidator<>).MakeGenericType(requestType);
            services.RegisterValidator(typeValidator);
            Validators.RegisterPropertyRulesFor(services, requestType, registerChildValidators);
        }
    }

    public static bool HasAsyncValidators(this IValidator validator, IValidationContext context, string ruleSet=null)
    {
        if (validator is IEnumerable<IValidationRule> rules)
        {
            foreach (var rule in rules)
            {
                if (ruleSet != null && rule.RuleSets != null && !rule.RuleSets.Contains(ruleSet))
                    continue;

                if (rule.Validators.Any(x => x is AsyncPredicateValidator || x is AsyncValidatorBase ||  x.ShouldValidateAsynchronously(context)))
                    return true;
            }
        }
        return false;
    }

    public static Task<List<ValidationRule>> GetAllValidateRulesAsync(this IResolver resolver, string type=null) =>
        resolver.TryResolve<IValidationSource>().GetAllValidateRulesAsync(type);
    public static async Task<List<ValidationRule>> GetAllValidateRulesAsync(this IValidationSource validationSource, string type=null)
    {
        return validationSource is IValidationSourceAdmin adminSource
            ? type != null 
                ? await adminSource.GetAllValidateRulesAsync(type).ConfigAwait()
                : await adminSource.GetAllValidateRulesAsync().ConfigAwait()
            : TypeConstants<ValidationRule>.EmptyList;
    }

    public static bool IsAuthValidator(this IValidateRule rule)
    {
        var validator = rule.Validator;
        if (!string.IsNullOrEmpty(validator))
        {
            return validator.StartsWith(nameof(ValidateScripts.IsAuthenticated))
                   || validator.StartsWith(nameof(ValidateScripts.IsAdmin))
                   || validator.StartsWith(nameof(ValidateScripts.HasRole))
                   || validator.StartsWith(nameof(ValidateScripts.HasPermission));
        }
        return false;
    }

    public static Operation ApplyValidationRules(this Operation op, IEnumerable<ValidationRule> rules)
    {
        var authRules = rules.Where(x => x.Type == op.Name && x.IsAuthValidator() && x.Field == null).ToList();
        if (authRules.Count > 0)
        {
            op = op.Clone();
            op.RequiresAuthentication = true;
            var appHost = HostContext.AppHost;
            var allValidators = authRules.Select(x => appHost.EvalExpression(x.Validator) as ITypeValidator).Where(x => x != null).ToList();
            if (allValidators.FirstOrDefault(x => x is HasRolesValidator) is HasRolesValidator roleValidator)
                roleValidator.Roles.Each(op.RequiredRoles.AddIfNotExists);
            if (allValidators.FirstOrDefault(x => x is HasPermissionsValidator) is HasPermissionsValidator permValidator)
                permValidator.Permissions.Each(op.RequiredPermissions.AddIfNotExists);
        }
        return op;
    }
        
    public static ScriptMethodType ToScriptMethodType(this ScriptMethodInfo scriptMethod) => new() {
        Name = scriptMethod.Name,
        ParamNames = scriptMethod.ParamNames,
        ParamTypes = scriptMethod.ParamTypes,
        ReturnType = scriptMethod.ReturnType,
    };
}