using System.Threading;
using System.Threading.Tasks;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.Text;

namespace ServiceStack.FluentValidation {

	public static class DefaultValidatorExtensionsServiceStack {

	    /// <summary>
	    /// Performs validation and then throws an exception if validation fails.
	    /// </summary>
	    /// <param name="validator">The validator this method is extending.</param>
	    /// <param name="instance">The instance of the type we are validating.</param>
	    /// <param name="applyTo">The ruleset to validate against.</param>
	    public static void ValidateAndThrow<T>(this IValidator<T> validator, T instance, ApplyTo applyTo)
	    {
		    var ruleSet = applyTo.ToString().ToUpper();
		    validator.Validate(instance, options => {
			    options.IncludeRuleSets(RulesetValidatorSelector.LegacyRulesetSplit(ruleSet));
			    options.ThrowOnFailures();
		    });
	    }

	    /// <summary>
	    /// Performs validation asynchronously and then throws an exception if validation fails.
	    /// </summary>
	    /// <param name="validator">The validator this method is extending.</param>
	    /// <param name="instance">The instance of the type we are validating.</param>
	    /// <param name="applyTo">Optional: a ruleset when need to validate against.</param>
	    /// <param name="token"></param>
	    public static async Task ValidateAndThrowAsync<T>(this IValidator<T> validator, T instance, ApplyTo applyTo, CancellationToken token=new())
	    {
		    var ruleSet = applyTo.ToString().ToUpper();
		    await validator.ValidateAsync(instance, options => {
			    options.IncludeRuleSets(RulesetValidatorSelector.LegacyRulesetSplit(ruleSet));
			    options.ThrowOnFailures();
		    }, token).ConfigAwait();
	    }

    }
}
