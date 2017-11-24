using System.Threading.Tasks;

namespace ServiceStack.FluentValidation {

	public static class DefaultValidatorExtensionsServiceStack {

	    /// <summary>
	    /// Performs validation and then throws an exception if validation fails.
	    /// </summary>
	    /// <param name="validator">The validator this method is extending.</param>
	    /// <param name="instance">The instance of the type we are validating.</param>
	    /// <param name="ruleSet">The ruleset to validate against.</param>
	    public static void ValidateAndThrow<T>(this IValidator<T> validator, T instance, ApplyTo ruleSet)
	    {
	        validator.ValidateAndThrow(instance, ruleSet.ToString().ToUpper());
	    }

	    /// <summary>
	    /// Performs validation asynchronously and then throws an exception if validation fails.
	    /// </summary>
	    /// <param name="validator">The validator this method is extending.</param>
	    /// <param name="instance">The instance of the type we are validating.</param>
	    /// <param name="ruleSet">Optional: a ruleset when need to validate against.</param>
	    public static Task ValidateAndThrowAsync<T>(this IValidator<T> validator, T instance, ApplyTo ruleSet)
	    {
	        return validator.ValidateAndThrowAsync(instance, ruleSet.ToString().ToUpper());
	    }

    }
}
