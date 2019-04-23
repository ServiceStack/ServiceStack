#if !NETSTANDARD2_0
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Validators;

namespace ServiceStack.FluentValidation.Mvc
{
	using System.Collections.Generic;
	using System.Web.Mvc;

	internal class EmailFluentValidationPropertyValidator : FluentValidationPropertyValidator
	{
		private IEmailValidator EmailValidator
		{
			get { return (IEmailValidator)Validator; }
		}

		public EmailFluentValidationPropertyValidator(ModelMetadata metadata, ControllerContext controllerContext, PropertyRule rule, IPropertyValidator validator)
			: base(metadata, controllerContext, rule, validator)
		{
			ShouldValidate = false;
		}

		public override IEnumerable<ModelClientValidationRule> GetClientValidationRules()
		{
			if (!ShouldGenerateClientSideRules()) yield break;

			var formatter = new MessageFormatter().AppendPropertyName(Rule.GetDisplayName());
			string message = formatter.BuildMessage(EmailValidator.ErrorMessageSource.GetString(Metadata));

			yield return new ModelClientValidationRule {
				ValidationType = "email",
				ErrorMessage = message
			};
		}
	}
}
#endif