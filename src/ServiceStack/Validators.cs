using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Resources;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Script;
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

        public override bool ShouldValidateAsynchronously(ValidationContext context) => true;
        //public override bool ShouldValidateAsync(ValidationContext context) => true;

        protected override async Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation)
        {
            var ret = await HostContext.AppHost.EvalScriptAsync(context.ToPageResult(Code), context.ParentContext.Request);
            return DefaultScripts.isTruthy(ret);
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var ret = HostContext.AppHost.EvalScript(context.ToPageResult(Code), context.ParentContext.Request);
            return DefaultScripts.isTruthy(ret);
        }
    }

    public static class Validators
    {
        public static Dictionary<Type, List<ITypeValidator>> TypeRulesMap { get; } =
            new Dictionary<Type, List<ITypeValidator>>();

        public static Dictionary<Type, List<IValidationRule>> TypePropertyRulesMap { get; } =
            new Dictionary<Type, List<IValidationRule>>();

        public static Dictionary<string, string> ConditionErrorCodes { get; } = new Dictionary<string, string>();
        public static Dictionary<string, string> ErrorCodeMessages { get; } = new Dictionary<string, string>();

        //TODO FV9: ValidatorOptions.Global.CascadeMode;
        static readonly Func<CascadeMode> CascadeMode = () => ValidatorOptions.CascadeMode;

        public static bool HasValidateRequestAttributes(Type type) => type.HasAttributeOf<ValidateRequestAttribute>();

        public static bool HasValidateAttributes(Type type) => type.GetPublicProperties().Any(x => x.HasAttributeOf<ValidateRequestAttribute>());

        public static async Task AssertTypeValidatorsAsync(IRequest req, object requestDto, Type requestType)
        {
            if (TypeRulesMap.TryGetValue(requestType, out var typeValidators))
            {
                foreach (var scriptValidator in typeValidators)
                {
                    await scriptValidator.ThrowIfNotValidAsync(requestDto, req);
                }
            }
        }
        
        static void ThrowNoValidator(string validator) =>
            throw new NotSupportedException(
                $"Could not resolve matching '{validator}` Validator Script Method. " +
                $"Ensure it's registered in AppHost.ScriptContext and called with correct number of arguments.");
        static void ThrowInvalidValidator(string validator, string validatorType) =>
            throw new NotSupportedException($"{validator} is not an `{validatorType}`");
        static void ThrowInvalidValidate() =>
            throw new NotSupportedException("[Validate] does not have a Validator or Condition");
        static void ThrowInvalidValidateRequest() =>
            throw new NotSupportedException("[ValidateRequest] does not have a Validator or Condition");

        public static bool RegisterRequestRulesFor(Type type)
        {
            var requestAttrs = type.AllAttributes();
            var to = new List<ITypeValidator>();

            foreach (var attr in requestAttrs.OfType<ValidateRequestAttribute>())
            {
                AddTypeValidator(to, attr);
            }

            if (to.Count > 0)
            {
                TypeRulesMap[type] = to;
                return true;
            }
            return false;
        }

        public static SharpPage ParseCondition(ScriptContext context, string condition)
        {
            var evalCode = ScriptCodeUtils.EnsureReturn(condition);
            return context.CodeSharpPage(evalCode);
        }

        public static void AddTypeValidator(List<ITypeValidator> to, IValidateRule attr)
        {
            var appHost = HostContext.AppHost;
            if (!string.IsNullOrEmpty(attr.Condition))
            {
                var code = ParseCondition(appHost.ScriptContext, attr.Condition);
                to.Add(new ScriptValidator(code, attr.Condition).Init(attr));
            }
            else if (!string.IsNullOrEmpty(attr.Validator))
            {
                var ret = appHost.EvalExpression(attr
                    .Validator); //Validators can't be cached due to custom code/msgs
                if (ret == null)
                    ThrowNoValidator(attr.Validator);

                if (ret is ITypeValidator validator)
                {
                    to.Add(validator.Init(attr));
                }
                else if (ret is List<object> objs)
                {
                    foreach (var o in objs)
                    {
                        if (o is ITypeValidator itemValidator)
                        {
                            to.Add(itemValidator.Init(attr));
                        }
                        else ThrowInvalidValidator(attr.Validator, nameof(ITypeValidator));
                    }
                }
                else ThrowInvalidValidator(attr.Validator, nameof(ITypeValidator));
            }
            else ThrowInvalidValidateRequest();
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

        public static List<ITypeValidator> GetTypeRules(Type type) => TypeRulesMap.TryGetValue(type, out var rules)
            ? rules
            : TypeConstants<ITypeValidator>.EmptyList;

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
                    rule.Validator += "(default('" + pi.PropertyType.Namespace + "." + pi.PropertyType.Name + "'))";
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
                    validator.Options.ErrorMessageSource = new StaticStringSource(propRule.Message.Localize());
                }
                else if (errorCode != null && ErrorCodeMessages.TryGetValue(errorCode, out var errorMsg))
                {
                    validator.Options.ErrorMessageSource = new StaticStringSource(errorMsg.Localize());
                }

                return validator;
            }

            if (propRule.Validator != null)
            {
                var ret = appHost.EvalExpression(propRule
                    .Validator); //Validators can't be cached due to custom code/msgs
                if (ret == null)
                    ThrowNoValidator(propRule.Validator);

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
                        else ThrowInvalidValidator(propRule.Validator, nameof(IPropertyValidator));
                    }
                }
                else ThrowInvalidValidator(propRule.Validator, nameof(IPropertyValidator));
            }
            else if (!string.IsNullOrEmpty(propRule.Condition))
            {
                var evalCode = ScriptCodeUtils.EnsureReturn(propRule.Condition);
                var page = appHost.ScriptContext.CodeSharpPage(evalCode);
                validators.Add(apply(new ScriptConditionValidator(page)));
            }
            else ThrowInvalidValidate();
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