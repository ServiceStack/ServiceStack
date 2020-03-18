using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Resources;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ScriptConditionValidator : PropertyValidator, IPredicateValidator
    {
        public SharpPage Code { get; }
        
        public ScriptConditionValidator(SharpPage code) : base(new LanguageStringSource(nameof(PredicateValidator)))
        {
            Code = code;
        }
        
        protected override bool IsValid(PropertyValidatorContext context)
        {
            var ret = HostContext.AppHost.EvalScript(context.ToPageResult(Code), context.ParentContext.Request);
            return DefaultScripts.isTruthy(ret);
        }
    }

    public interface ITypeValidationRule 
    {
        bool IsValid(object dto, IRequest request = null);
        void ThrowIfNotValid(object dto, IRequest request = null);
    }

    public class ScriptValidator : ITypeValidationRule
    {
        public static string DefaultErrorCode { get; set; } = "InvalidRequest";
        public static string DefaultErrorMessage { get; set; } = "`The specified condition was not met for '${TypeName}'.`";

        public SharpPage Code { get; }
        public string Condition { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        
        public int StatusCode { get; set; }
        
        public ScriptValidator(SharpPage code, string condition, string errorCode=null, string message=null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            ErrorCode = errorCode;
            Message = message;
        }

        public bool IsValid(object dto, IRequest request = null)
        {
            var pageResult = new PageResult(Code) {
                Args = {
                    [ScriptConstants.It] = dto,
                }
            };
            var ret = HostContext.AppHost.EvalScript(pageResult, request);
            return DefaultScripts.isTruthy(ret);
        }

        public void ThrowIfNotValid(object dto, IRequest request = null)
        {
            if (IsValid(dto, request))
                return;

            var appHost = HostContext.AppHost;

            var errorCode = ErrorCode ?? DefaultErrorCode;
            var messageExpr = Message != null
                ? appHost.ResolveLocalizedString(Message)
                : Validators.ErrorCodeMessages.TryGetValue(errorCode, out var msg)
                    ? appHost.ResolveLocalizedString(msg)
                    : appHost.ResolveLocalizedString(DefaultErrorMessage);

            string errorMsg = messageExpr;
            if (messageExpr.IndexOf('`') >= 0)
            {
                var msgToken = JS.expressionCached(appHost.ScriptContext, messageExpr);
                errorMsg = (string) msgToken.Evaluate(JS.CreateScope(new Dictionary<string, object> {
                    [ScriptConstants.It] = dto,
                    [ScriptConstants.Request] = request,
                    ["TypeName"] = dto.GetType().Name,
                })) ?? DefaultErrorMessage;
            }

            var statusCode = StatusCode >= 400
                ? StatusCode
                : 400; //BadRequest
            
            throw new HttpError(statusCode, errorCode, errorMsg);
        }
    }
    
    public static class Validators
    {
        public static NullValidator Null { get; } = new NullValidator();
        public static NotNullValidator NotNull { get; } = new NotNullValidator();
        public static NotEmptyValidator NotEmpty { get; } = new NotEmptyValidator(null);
        public static EmptyValidator Empty { get; } = new EmptyValidator(null);

        public static CreditCardValidator CreditCard { get; } = new CreditCardValidator();
        public static EmailValidator Email { get; } = new EmailValidator();

        public static Dictionary<Type, List<ITypeValidationRule>> TypeRulesMap { get; } =
            new Dictionary<Type, List<ITypeValidationRule>>();

        public static Dictionary<Type, List<IValidationRule>> TypePropertyRulesMap { get; } =
            new Dictionary<Type, List<IValidationRule>>();

        public static Dictionary<string, string> ConditionErrorCodes { get; } = new Dictionary<string, string>();
        public static Dictionary<string, string> ErrorCodeMessages { get; } = new Dictionary<string, string>();

        //TODO FV9: ValidatorOptions.Global.CascadeMode;
        static readonly Func<CascadeMode> CascadeMode = () => ValidatorOptions.CascadeMode;

        public static bool HasValidateRequestAttributes(Type type) => type.HasAttribute<ValidateRequestAttribute>();

        public static bool HasValidateAttributes(Type type) => type.GetPublicProperties().Any(x => x.HasAttribute<ValidateAttribute>());

        public static void AssertTypeValidators(IRequest req, object requestDto, Type requestType)
        {
            if (TypeRulesMap.TryGetValue(requestType, out var typeValidators))
            {
                foreach (var scriptValidator in typeValidators)
                {
                    scriptValidator.ThrowIfNotValid(requestDto, req);
                }
            }
        }

        public static bool RegisterRequestRulesFor(Type type)
        {
            var appHost = HostContext.AppHost;
            var requestAttrs = type.AllAttributes<ValidateRequestAttribute>();
            var to = new List<ITypeValidationRule>();

            foreach (var attr in requestAttrs)
            {
                if (string.IsNullOrEmpty(attr.Condition))
                    continue;
                
                var evalCode = ScriptCodeUtils.EnsureReturn(attr.Condition);
                var code = appHost.ScriptContext.CodeSharpPage(evalCode);
                to.Add(new ScriptValidator(code, attr.Condition, attr.ErrorCode, attr.Message) {
                    StatusCode = attr.StatusCode,
                });
            }

            if (to.Count > 0)
            {
                TypeRulesMap[type] = to;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Register declarative property [Validate] attributes.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool RegisterPropertyRulesFor(Type type)
        {
            var typeRules = new List<IValidationRule>();
            foreach (var pi in type.GetPublicProperties())
            {
                var allAttrs = pi.AllAttributes();
                var validateAttrs = allAttrs.Where(x => x is ValidateAttribute).ToList();

                if (validateAttrs.Count > 0)
                {
                    var rule = CreatePropertyRule(type, pi);
                    typeRules.Add(rule);
                    var validators = (List<IPropertyValidator>) rule.Validators;

                    foreach (ValidateAttribute attr in validateAttrs)
                    {
                        validators.AddRule(pi, attr);
                    }
                }
            }

            if (typeRules.Count > 0)
            {
                TypePropertyRulesMap[type] = typeRules;
                return true;
            }

            return false;
        }

        public static List<ITypeValidationRule> GetTypeRules(Type type) => TypeRulesMap.TryGetValue(type, out var rules)
            ? rules
            : TypeConstants<ITypeValidationRule>.EmptyList;

        public static List<IValidationRule> GetPropertyRules(Type type) => TypePropertyRulesMap.TryGetValue(type, out var rules)
            ? rules
            : TypeConstants<IValidationRule>.EmptyList;

        public static void AddRule(Type type, string name, ValidateAttribute attr) =>
            AddRule(type, type.GetProperty(name), attr);

        public static void AddRule(Type type, PropertyInfo pi, ValidateAttribute attr)
        {
            var typeRules = TypePropertyRulesMap.TryGetValue(type, out var rules)
                ? rules
                : TypePropertyRulesMap[type] = new List<IValidationRule>();

            var rule = typeRules.FirstOrDefault(x => (x as PropertyRule)?.PropertyName == pi.Name);
            if (rule == null)
                typeRules.Add(rule = CreatePropertyRule(type, pi));

            var validators = (List<IPropertyValidator>) rule.Validators;
            validators.AddRule(pi, attr);
        }

        public static IValidationRule CreatePropertyRule(Type type, PropertyInfo pi)
        {
            var fn = pi.CreateGetter();
            return new PropertyRule(pi, x => fn(x), null, CascadeMode, type, null);
        }

        public static List<Action<PropertyInfo, IValidateRule>> RuleFilters { get; } =
            new List<Action<PropertyInfo, IValidateRule>> {
                AppendDefaultValueOnEmptyValidators,
            };

        public static void AppendDefaultValueOnEmptyValidators(PropertyInfo pi, IValidateRule rule)
        {
            if (rule.Validator == "Empty" || rule.Validator == "NotEmpty")
            {
                // Not/EmptyValidator has a required default constructor required to accurately determine empty for value types
                if (pi.PropertyType.IsValueType && !pi.PropertyType.IsNullableType())
                {
                    rule.Validator += "(default('" + pi.PropertyType.Namespace + "." + pi.PropertyType.Name + "')";
                }
            }
        }

        public static void AddRule(this List<IPropertyValidator> validators, PropertyInfo pi, IValidateRule propRule)
        {
            var appHost = HostContext.AppHost;
            foreach (var ruleFilter in RuleFilters)
            {
                ruleFilter(pi, propRule);
            }

            IPropertyValidator apply(IPropertyValidator validator)
            {
                var errorCode = propRule.ErrorCode;
                if (!string.IsNullOrEmpty(errorCode))
                {
                    validator.Options.ErrorCodeSource = new StaticStringSource(errorCode);
                }
                else
                {
                    if (propRule.Condition != null &&
                        ConditionErrorCodes.TryGetValue(propRule.Condition, out errorCode))
                    {
                        validator.Options.ErrorCodeSource = new StaticStringSource(errorCode);
                    }
                }

                if (!string.IsNullOrEmpty(propRule.Message))
                {
                    validator.Options.ErrorMessageSource =
                        new StaticStringSource(appHost.ResolveLocalizedString(propRule.Message));
                }
                else if (errorCode != null && ErrorCodeMessages.TryGetValue(errorCode, out var errorMsg))
                {
                    validator.Options.ErrorMessageSource =
                        new StaticStringSource(appHost.ResolveLocalizedString(errorMsg));
                }

                return validator;
            }

            if (propRule.Validator != null)
            {
                var ret = appHost.EvalExpression(propRule
                    .Validator); //Validators can't be cached due to custom code/msgs
                if (ret == null)
                {
                    throw new NotSupportedException(
                        $"Could not resolve matching '{propRule.Validator}` Validator Script Method. " +
                        $"Ensure it's registered in AppHost.ScriptContext and called with correct number of arguments.");
                }

                if (ret is IPropertyValidator validator)
                {
                    validators.Add(apply(validator));
                }
                else if (ret is List<object> objs)
                {
                    foreach (var o in objs)
                    {
                        if (o is IPropertyValidator itemValidator)
                        {
                            validators.Add(apply(itemValidator));
                        }
                    }
                }
                else throw new NotSupportedException($"{propRule.Validator} is not an IPropertyValidator");
            }
            else if (!string.IsNullOrEmpty(propRule.Condition))
            {
                var evalCode = ScriptCodeUtils.EnsureReturn(propRule.Condition);
                var page = appHost.ScriptContext.CodeSharpPage(evalCode);
                validators.Add(apply(new ScriptConditionValidator(page)));
            }
        }

        public static PageResult ToPageResult(this PropertyValidatorContext context, SharpPage page)
        {
            var to = new PageResult(page) {
                Args = {
                    [ScriptConstants.It] = context.PropertyValue,
                    [ScriptConstants.Field] = context.PropertyName,
                }
            };
            return to;
        }
    }
}