#if !NETSTANDARD2_0
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Resources;
using ServiceStack.FluentValidation.Validators;

namespace FluentValidation.Mvc {
	using System.Collections.Generic;
	using System.Web.Mvc;

	internal class RangeFluentValidationPropertyValidator : FluentValidationPropertyValidator {
		InclusiveBetweenValidator RangeValidator {
			get { return (InclusiveBetweenValidator)Validator; }
		}
		
		public RangeFluentValidationPropertyValidator(ModelMetadata metadata, ControllerContext controllerContext, PropertyRule propertyDescription, IPropertyValidator validator) : base(metadata, controllerContext, propertyDescription, validator) {
			ShouldValidate=false;
		}

		public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
			if (!ShouldGenerateClientSideRules()) yield break;

			var formatter = new MessageFormatter()
				.AppendPropertyName(Rule.GetDisplayName())
				.AppendArgument("From", RangeValidator.From)
				.AppendArgument("To", RangeValidator.To);

			string message = RangeValidator.ErrorMessageSource.GetString(Metadata);

			message = formatter.BuildMessage(message);

			yield return new ModelClientValidationRangeRule(message, RangeValidator.From, RangeValidator.To);
		}
	}
}
#endif