using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Script;

namespace ServiceStack
{
    public static class Validators
    {
        public static NullValidator Null { get; } = new NullValidator();
        public static NotNullValidator NotNull { get; } = new NotNullValidator();
        public static NotEmptyValidator NotEmpty { get; } = new NotEmptyValidator(null);
        public static EmptyValidator Empty { get; } = new EmptyValidator(null);
        
        public static CreditCardValidator CreditCard { get; } = new CreditCardValidator();
        public static EmailValidator Email { get; } = new EmailValidator();

        public static Dictionary<Type, List<IValidationRule>> TypeRulesMap { get; } = new Dictionary<Type, List<IValidationRule>>();

        static readonly Func<CascadeMode> CascadeMode = () => ValidatorOptions.CascadeMode;

        public static bool HasValidationAttributes(Type type) => type.HasAttribute<ValidateRequestAttribute>() || 
            type.GetPublicProperties().FirstOrDefault(x => x.HasAttribute<ValidateAttribute>()) != null;
        
        /// <summary>
        /// Register declarative [Validate] validators.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool RegisterValidator(Type type)
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
                TypeRulesMap[type] = typeRules;
                return true;
            }
            return false;
        }

        public static List<IValidationRule> GetRules(Type type) => TypeRulesMap.TryGetValue(type, out var rules)
            ? rules
            : TypeConstants<IValidationRule>.EmptyList; 

        public static void AddRule(Type type, string name, ValidateAttribute attr) =>
            AddRule(type, type.GetProperty(name), attr);

        public static void AddRule(Type type, PropertyInfo pi, ValidateAttribute attr)
        {
            var typeRules = TypeRulesMap.TryGetValue(type, out var rules)
                ? rules
                : TypeRulesMap[type] = new List<IValidationRule>();

            var rule = typeRules.FirstOrDefault(x => (x as PropertyRule)?.PropertyName == pi.Name);
            if (rule == null)
                typeRules.Add(rule = CreatePropertyRule(type, pi));

            var validators = (List<IPropertyValidator>) rule.Validators;
            validators.AddRule(pi, attr);
        }

        private static IValidationRule CreatePropertyRule(Type type, PropertyInfo pi)
        {
            var fn = pi.CreateGetter();
            return new PropertyRule(pi, x => fn(x), null, CascadeMode, type, null);
        }
        
        public static List<Action<PropertyInfo,ValidateAttribute>> RuleFilters { get; } = new List<Action<PropertyInfo,ValidateAttribute>>{
            AppendDefaultValueOnEmptyValidators,
        };

        public static void AppendDefaultValueOnEmptyValidators(PropertyInfo pi, ValidateAttribute attr)
        {
            if (attr.Validator == "Empty" || attr.Validator == "NotEmpty")
            {
                // Not/EmptyValidator has a required default constructor required to accurately determine empty for value types
                if (pi.PropertyType.IsValueType && !pi.PropertyType.IsNullableType())
                {
                    attr.Validator += "(default('" + pi.PropertyType.Namespace + "." + pi.PropertyType.Name + "')";
                }
            }
        }

        public static void AddRule(this List<IPropertyValidator> validators, PropertyInfo pi, ValidateAttribute attr)
        {
            foreach (var ruleFilter in RuleFilters)
            {
                ruleFilter(pi, attr);
            }

            if (attr.Validator != null)
            {
                var ret = HostContext.AppHost.EvalExpressionCached(attr.Validator);
                if (ret == null)
                {
                    throw new NotSupportedException(
                        $"Could not resolve matching '{attr.Validator}` Validator Script Method. " +
                        $"Ensure it's registered in AppHost.ScriptContext and called with correct number of arguments.");
                }                
                    
                if (ret is IPropertyValidator validator)
                {
                    validators.Add(validator);
                }
                else if (ret is List<object> objs)
                {
                    foreach (var o in objs)
                    {
                        if (o is IPropertyValidator itemValidator)
                        {
                            validators.Add(itemValidator);
                        }
                    }
                }
                else throw new NotSupportedException($"{attr.Validator} is not an IPropertyValidator");
            }
        }
    }

    public class ValidateScripts : ScriptMethods
    {
        public static ValidateScripts Instance = new ValidateScripts();

        public IPropertyValidator Null() => Validators.Null;
        public IPropertyValidator Empty() => Validators.Empty;
        public IPropertyValidator Empty(object defaultValue) => new EmptyValidator(defaultValue);
        public IPropertyValidator Equal(object value) => new EqualValidator(value);
        public IPropertyValidator NotNull() => Validators.NotNull;
        public IPropertyValidator NotEmpty() => Validators.NotEmpty;
        public IPropertyValidator NotEmpty(object defaultValue) => new NotEmptyValidator(defaultValue);
        public IPropertyValidator NotEqual(object value) => new NotEqualValidator(value);

        public IPropertyValidator CreditCard() => Validators.CreditCard;
        public IPropertyValidator Email() => Validators.Email;

        public IPropertyValidator Length(int min, int max) => new LengthValidator(min, max);
        public IPropertyValidator ExactLength(int length) => new ExactLengthValidator(length);
        public IPropertyValidator MaximumLength(int max) => new MaximumLengthValidator(max);
        public IPropertyValidator MinimumLength(int min) => new MinimumLengthValidator(min);
        public IPropertyValidator InclusiveBetween(IComparable from, IComparable to) => new InclusiveBetweenValidator(from,to);
        public IPropertyValidator ExclusiveBetween(IComparable from, IComparable to) => new ExclusiveBetweenValidator(from,to);

        public IPropertyValidator LessThan(int value) => new LessThanValidator(value);
        public IPropertyValidator LessThanOrEqual(int value) => new LessThanOrEqualValidator(value);
        public IPropertyValidator GreaterThan(int value) => new GreaterThanValidator(value);
        public IPropertyValidator GreaterThanOrEqual(int value) => new GreaterThanOrEqualValidator(value);
        public IPropertyValidator ScalePrecision(int scale, int precision) => new ScalePrecisionValidator(scale, precision);

        public IPropertyValidator RegularExpression(string regex) => new RegularExpressionValidator(regex, RegexOptions.Compiled);
        
        public IPropertyValidator Enum(Type enumType) => new EnumValidator(enumType);
    }
}
