using System;
using ServiceStack.ServiceHost;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Validation
{
	public class ValidationFilter
	{
		public void ValidateRequest(IHttpRequest req, IHttpResponse res, object requestDto)
		{
			var validator = ValidatorCache.GetValidator(req, requestDto.GetType());
			if (validator != null)
			{
				string ruleSet = req.HttpMethod;
				var validationResult = validator.Validate(
					new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)));

				if (validationResult.IsValid) return;
				
				var responseStatus = ResponseStatusTranslator.Instance.Parse(validationResult.AsSerializable());

				var errorResponse = ServiceUtils.CreateErrorResponse(
					requestDto, 
					new ValidationError(validationResult.AsSerializable()),
					responseStatus);

				res.WriteToResponse(req, errorResponse);
			}
		}		
	}
}
