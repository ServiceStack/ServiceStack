using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Host;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public class ValidationFeature : IPlugin, IPostInitPlugin, IAfterInitAppHost, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Validation;
        public Func<IRequest, ValidationResult, object, object> ErrorResponseFilter { get; set; }

        public bool ScanAppHostAssemblies { get; set; } = true;
        public bool TreatInfoAndWarningsAsErrors { get; set; } = true;
        public bool EnableDeclarativeValidation { get; set; } = true;

        public bool ImplicitlyValidateChildProperties { get; set; } = true;
        public string AccessRole { get; set; } = RoleNames.Admin;
        
        public IValidationSource ValidationSource { get; set; } 
        
        public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
            { typeof(GetValidationRulesService), new []{ "/" + "validation/rules".Localize() + "/{Type}" } },
            { typeof(ModifyValidationRulesService), new []{ "/" + "validation/rules".Localize() } },
        };

        /// <summary>
        /// Specify default ErrorCodes to use when custom validation conditions are invalid
        /// </summary>
        public Dictionary<string, string> ConditionErrorCodes => Validators.ConditionErrorCodes;

        /// <summary>
        /// Specify default Error Messages to use when Validators with these ErrorCode's are invalid
        /// </summary>
        public Dictionary<string, string> ErrorCodeMessages => Validators.ErrorCodeMessages;
        
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
            }

            if (ValidationSource != null)
            {
                appHost.Register(ValidationSource);
                ValidationSource.InitSchema();
            }

            var container = appHost.GetContainer();
            var hasValidationSource = ValidationSource != null || container.Exists<IValidationSource>(); 
            if (hasValidationSource && AccessRole != null)
            {
                appHost.RegisterServices(ServiceRoutes);
            }

            appHost.AddToAppMetadata(metadata => {
                metadata.Plugins.Validation = new ValidationInfo {
                    HasValidationSource = hasValidationSource.NullIfFalse(), 
                    HasValidationSourceAdmin = (container.TryResolve<IValidationSource>() is IValidationSourceAdmin).NullIfFalse(),
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
            
            appHost.GetPlugin<MetadataFeature>()?.ExportTypes.Add(typeof(ValidationRule));
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            if (ScanAppHostAssemblies)
            {
                appHost.GetContainer().RegisterValidators(((ServiceStackHost)appHost).ServiceAssemblies.ToArray());
            }
        }

        public void AfterInit(IAppHost appHost)
        {
            if (EnableDeclarativeValidation)
            {
                var container = appHost.GetContainer();
                var hasDynamicRules = ValidationSource != null || container.Exists<IValidationSource>(); 
                
                foreach (var op in appHost.Metadata.Operations)
                {
                    var hasValidateRequestAttrs = Validators.HasValidateRequestAttributes(op.RequestType);
                    if (hasValidateRequestAttrs)
                    {
                        Validators.RegisterRequestRulesFor(op.RequestType);
                        op.AddRequestTypeValidationRules(Validators.GetTypeRules(op.RequestType));
                    }
                        
                    var hasValidateAttrs = Validators.HasValidateAttributes(op.RequestType);
                    if (hasDynamicRules || hasValidateAttrs)
                    {
                        container.RegisterNewValidatorIfNotExists(op.RequestType);
                        op.AddRequestPropertyValidationRules(Validators.GetPropertyRules(op.RequestType));
                    }
                }
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
    }

    [DefaultRequest(typeof(GetValidationRules))]
    [Restrict(VisibilityTo = RequestAttributes.Localhost)]
    public class GetValidationRulesService : Service
    {
        public IValidationSource ValidationSource { get; set; }
        public async Task<object> Any(GetValidationRules request)
        {
            var feature = HostContext.AssertPlugin<ValidationFeature>();
            await RequestUtils.AssertAccessRoleAsync(base.Request, accessRole: feature.AccessRole, authSecret: request.AuthSecret).ConfigAwait();

            var type = HostContext.Metadata.FindDtoType(request.Type);
            if (type == null)
                throw HttpError.NotFound(request.Type);
            
            return new GetValidationRulesResponse {
                Results = await ValidationSource.GetAllValidateRulesAsync(request.Type).ConfigAwait(),
            };
        }
    }

    [DefaultRequest(typeof(ModifyValidationRules))]
    [Restrict(VisibilityTo = RequestAttributes.Localhost)]
    public class ModifyValidationRulesService : Service
    {
        public IValidationSource ValidationSource { get; set; }

        public async Task Any(ModifyValidationRules request)
        {
            var appHost = HostContext.AssertAppHost();
            var feature = appHost.AssertPlugin<ValidationFeature>();
            await RequestUtils.AssertAccessRoleAsync(base.Request, accessRole: feature.AccessRole, authSecret: request.AuthSecret).ConfigAwait();

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

                await ValidationSource.SaveValidationRulesAsync(rules).ConfigAwait();
            }

            if (!request.SuspendRuleIds.IsEmpty())
            {
                var suspendRules = await ValidationSource.GetValidateRulesByIdsAsync(request.SuspendRuleIds).ConfigAwait();
                foreach (var suspendRule in suspendRules)
                {
                    suspendRule.SuspendedBy = userName;
                    suspendRule.SuspendedDate = utcNow;
                }

                await ValidationSource.SaveValidationRulesAsync(suspendRules).ConfigAwait();
            }

            if (!request.UnsuspendRuleIds.IsEmpty())
            {
                var unsuspendRules = await ValidationSource.GetValidateRulesByIdsAsync(request.UnsuspendRuleIds).ConfigAwait();
                foreach (var unsuspendRule in unsuspendRules)
                {
                    unsuspendRule.SuspendedBy = null;
                    unsuspendRule.SuspendedDate = null;
                }

                await ValidationSource.SaveValidationRulesAsync(unsuspendRules).ConfigAwait();
            }

            if (!request.DeleteRuleIds.IsEmpty())
            {
                await ValidationSource.DeleteValidationRulesAsync(request.DeleteRuleIds.ToArray()).ConfigAwait();
            }

            if (request.ClearCache.GetValueOrDefault())
            {
                await ValidationSource.ClearCacheAsync().ConfigAwait();
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
            RegisteredDtoValidators = new HashSet<Type>();
            TypesValidatorsMap = new Dictionary<Type, Type>();
            ValidatorTypes = new List<Type>();
            RegisteredAssemblies = new List<Assembly>();
            RegisteredValidators = new List<Type>();
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
                        if (genericValidator != null)
                        {
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
        /// <param name="container">The IoC container</param>
        /// <param name="assemblies">The assemblies to scan for a validator</param>
        public static void RegisterValidators(this Container container, params Assembly[] assemblies)
        {
            RegisterValidators(container, ReuseScope.None, assemblies);
        }

        public static void RegisterValidators(this Container container, ReuseScope scope, params Assembly[] assemblies)
        {
            Init(assemblies);
            var validatorTypesSnapshot = ValidatorTypes.ToArray();
            foreach (var validatorType in validatorTypesSnapshot)
            {
                container.RegisterValidator(validatorType, scope);
            }
        }

        public static void RegisterValidator(this Container container, Type validator, ReuseScope scope=ReuseScope.None)
        {
            if (RegisteredValidators.Contains(validator))
                return;

            var baseType = validator.BaseType;
            if (validator.IsInterface || baseType == null)
                return;

            while (baseType != null && !baseType.IsGenericType)
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

            container.RegisterAutoWiredType(validator, validatorType, scope);

            Validators.RegisterPropertyRulesFor(dtoType);
            RegisteredDtoValidators.Add(dtoType);
        }

        internal static void RegisterNewValidatorIfNotExists(this Container container, Type requestType)
        {
            // We only need to register a new a Validator if it doesn't already exist for the Type 
            if (!RegisteredDtoValidators.Contains(requestType))
            {
                var typeValidator = typeof(DefaultValidator<>).MakeGenericType(requestType);
                container.RegisterValidator(typeValidator);
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
}
