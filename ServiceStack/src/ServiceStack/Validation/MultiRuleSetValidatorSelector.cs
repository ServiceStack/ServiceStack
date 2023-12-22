using System.Linq;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation;

namespace ServiceStack.Validation;

public class MultiRuleSetValidatorSelector : IValidatorSelector
{
    readonly string[] rulesetsToExecute;

    /// <summary>
    /// Creates a new instance of the RulesetValidatorSelector.
    /// </summary>
    public MultiRuleSetValidatorSelector(params string[] rulesetsToExecute) {
        this.rulesetsToExecute = rulesetsToExecute;
    }

    /// <summary>
    /// Determines whether or not a rule should execute.
    /// </summary>
    /// <param name="rule">The rule</param>
    /// <param name="propertyPath">Property path (eg Customer.Address.Line1)</param>
    /// <param name="context">Contextual information</param>
    /// <returns>Whether or not the validator can execute.</returns>
    public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context) {
        if (rule.RuleSets == null || rule.RuleSets.Length == 0) return true;
        if (rulesetsToExecute.Contains("*")) return true;

        foreach (var ruleset in rule.RuleSets)
        {
            if (!string.IsNullOrEmpty(ruleset) && rulesetsToExecute.Length > 0 && 
                rulesetsToExecute.Contains(ruleset)) return true;
        }

        return false;
    }
}