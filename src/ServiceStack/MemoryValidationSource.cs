using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack
{
    public class MemoryValidationSource : IValidationSource, IValidationSourceWriter
    {
        public static readonly ConcurrentDictionary<string, KeyValuePair<string, IValidateRule>[]> TypeRulesMap =
            new ConcurrentDictionary<string, KeyValuePair<string, IValidateRule>[]>();

        public IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type)
        {
            return TypeRulesMap.TryGetValue(type.Name, out var rules)
                ? rules
                : TypeConstants<KeyValuePair<string, IValidateRule>>.EmptyArray;
        }

        private readonly object semaphore = new object();

        public void SaveValidationRules(List<ValidateRule> validateRules)
        {
            lock (semaphore)
            {
                var typeGroup = validateRules.ToLookup(x => x.Type);
                foreach (var group in typeGroup)
                {
                    var propMap = new Dictionary<string, ValidateRule>();
                    if (TypeRulesMap.TryGetValue(group.Key, out var typeRules))
                    {
                        foreach (var entry in typeRules)
                        {
                            var rule = (ValidateRule) entry.Value;
                            propMap[rule.Field] = rule;
                        }
                    }

                    foreach (var newRule in group)
                    {
                        propMap[newRule.Field] = newRule; //override property rule
                    }

                    var newTypeRules = propMap.Values.OrderBy(x => x.SortOrder)
                        .Select(x => new KeyValuePair<string,IValidateRule>(x.Field, x))
                        .ToArray();
                    TypeRulesMap[group.Key] = newTypeRules;
                }
            }
        }

        public void Clear() => TypeRulesMap.Clear();
    }


    public static class ValidationSourceUtils
    {
        public static void InitSchema(this IValidationSource source)
        {
            if (source is IRequiresSchema requiresSchema)
            {
                requiresSchema.InitSchema();
            }
        }

        public static void SaveValidationRules(this IValidationSource source, List<ValidateRule> validateRules)
        {
            if (source is IValidationSourceWriter sourceWriter)
            {
                sourceWriter.SaveValidationRules(validateRules);
            }
            else throw new NotSupportedException($"{source.GetType().Name} does not implement IValidationSourceWriter");
        }
    }
}