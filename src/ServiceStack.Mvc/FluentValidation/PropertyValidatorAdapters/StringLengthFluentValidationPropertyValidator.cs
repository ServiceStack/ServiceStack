#if !NETSTANDARD2_0
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Resources;
using ServiceStack.FluentValidation.Validators;

namespace FluentValidation.Mvc {
	using System.Collections.Generic;
	using System.Web.Mvc;

	internal class StringLengthFluentValidationPropertyValidator : FluentValidationPropertyValidator {
		private ILengthValidator LengthValidator {
			get { return (ILengthValidator)Validator; }
		}

		public StringLengthFluentValidationPropertyValidator(ModelMetadata metadata, ControllerContext controllerContext, PropertyRule rule, IPropertyValidator validator)
			: base(metadata, controllerContext, rule, validator) {
			ShouldValidate = false;
		}

		public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
			if(!ShouldGenerateClientSideRules()) yield break;

			var formatter = new MessageFormatter()
				.AppendPropertyName(Rule.GetDisplayName())
				.AppendArgument("MinLength", LengthValidator.Min)
				.AppendArgument("MaxLength", LengthValidator.Max);

			string message = LengthValidator.ErrorMessageSource.GetString(Metadata);

			message = formatter.BuildMessage(message);

			yield return new ModelClientValidationStringLengthRule(message, LengthValidator.Min, LengthValidator.Max) ;
		}
	}
}
#endif
