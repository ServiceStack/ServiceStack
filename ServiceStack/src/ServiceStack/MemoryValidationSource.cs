using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack;

public class MemoryValidationSource : IValidationSource, IValidationSourceAdmin, IClearable
{
    public static readonly ConcurrentDictionary<string, KeyValuePair<string, IValidateRule>[]> TypeRulesMap = new();

    public IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type)
    {
        var ret = TypeRulesMap.TryGetValue(type.Name, out var rules)
            ? rules.Where(x => ((ValidationRule)x.Value).SuspendedDate == null).ToArray()
            : TypeConstants<KeyValuePair<string, IValidateRule>>.EmptyArray;
            
        return ret;
    }

    private readonly object semaphore = new();
    internal static int IdCounter;

    public List<ValidationRule> GetAllValidateRules()
    {
        var rules = TypeRulesMap.Values.SelectMany(x => x.Select(y => (ValidationRule)y.Value));
        return rules.ToList();
    }

    public Task<List<ValidationRule>> GetAllValidateRulesAsync() => GetAllValidateRules().InTask(); 

    public Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName)
    {
        var ret = TypeRulesMap.TryGetValue(typeName, out var rules)
            ? rules
            : TypeConstants<KeyValuePair<string, IValidateRule>>.EmptyArray;
            
        return ret.Map(x => (ValidationRule)x.Value).InTask();
    }

    public void SaveValidationRules(List<ValidationRule> validateRules)
    {
        lock (semaphore)
        {
            var typeGroup = validateRules.ToLookup(x => x.Type);
            foreach (var group in typeGroup)
            {
                var typeRules = TypeRulesMap.TryGetValue(group.Key, out var existingRules)
                    ? new List<KeyValuePair<string, IValidateRule>>(existingRules)
                    : new List<KeyValuePair<string, IValidateRule>>();

                    
                foreach (var rule in group)
                {
                    if (rule.Id != default && typeRules.Any(x => ((ValidationRule) x.Value).Id == rule.Id))
                    {
                        var existingRule = typeRules.First(x => ((ValidationRule) x.Value).Id == rule.Id);
                        typeRules.Remove(existingRule);
                        typeRules.Add(new KeyValuePair<string, IValidateRule>(existingRule.Key, rule));
                    }
                    else
                    {
                        rule.Id = Interlocked.Increment(ref IdCounter);
                        typeRules.Add(new KeyValuePair<string, IValidateRule>(rule.Field, rule));
                    }
                }

                var newTypeRules = typeRules.OrderBy(x => ((ValidationRule)x.Value).Id)
                    .ToArray();
                TypeRulesMap[group.Key] = newTypeRules;
            }
        }
    }

    public Task SaveValidationRulesAsync(List<ValidationRule> validateRules)
    {
        SaveValidationRules(validateRules);
        return TypeConstants.EmptyTask;
    }

    public Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids)
    {
        var to = new List<ValidationRule>();
        foreach (var entry in TypeRulesMap)
        {
            foreach (var propRule in entry.Value)
            {
                if (propRule.Value is ValidationRule rule && ids.Contains(rule.Id))
                    to.Add(rule);
            }
        }
        return Task.FromResult(to);
    }

    public Task DeleteValidationRulesAsync(params int[] ids)
    {
        lock (semaphore)
        {
            var replace = new Dictionary<string, KeyValuePair<string, IValidateRule>[]>();
            foreach (var entry in TypeRulesMap)
            {
                if (entry.Value.Any(x => ids.Contains(((ValidationRule) x.Value).Id)))
                {
                    replace[entry.Key] = entry.Value
                        .Where(x => !ids.Contains(((ValidationRule) x.Value).Id)).ToArray();
                }
            }

            foreach (var entry in replace)
            {
                TypeRulesMap[entry.Key] = entry.Value;
            }
        }
        return TypeConstants.EmptyTask;
    }

    public Task ClearCacheAsync() => TypeConstants.EmptyTask;

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

    public static async Task<List<ValidationRule>> GetAllValidateRulesAsync(this IValidationSource source, string typeName)
    {
        if (source is IValidationSourceAdmin sourceAdmin)
            return await sourceAdmin.GetAllValidateRulesAsync(typeName).ConfigAwait();

        ThrowNotValidationSourceAdmin(source);
        return null;
    }

    public static Task SaveValidationRulesAsync(this IValidationSource source, List<ValidationRule> validateRules)
    {
        if (source is IValidationSourceAdmin sourceAdmin)
            return sourceAdmin.SaveValidationRulesAsync(validateRules);
            
        ThrowNotValidationSourceAdmin(source);
        return null;
    }

    public static async Task DeleteValidationRulesAsync(this IValidationSource source, params int[] ids)
    {
        if (source is IValidationSourceAdmin sourceAdmin)
            await sourceAdmin.DeleteValidationRulesAsync(ids).ConfigAwait();
        else
            ThrowNotValidationSourceAdmin(source);
    }

    public static async Task ClearCacheAsync(this IValidationSource source, params int[] ids)
    {
        if (source is IValidationSourceAdmin sourceAdmin)
            await sourceAdmin.ClearCacheAsync().ConfigAwait();
        else
            ThrowNotValidationSourceAdmin(source);
    }

    public static async Task<List<ValidationRule>> GetValidateRulesByIdsAsync(this IValidationSource source, params int[] ids)
    {
        if (source is IValidationSourceAdmin sourceAdmin)
            return await sourceAdmin.GetValidateRulesByIdsAsync(ids).ConfigAwait();

        ThrowNotValidationSourceAdmin(source);
        return null;
    }

    private static void ThrowNotValidationSourceAdmin(IValidationSource source) => 
        throw new NotSupportedException($"{source.GetType().Name} does not implement IValidationSourceAdmin");
}