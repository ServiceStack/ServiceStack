using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using FluentValidation;
using System.Reflection;
using FluentValidation.Internal;
using ServiceStack.Text;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Common.Utils;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Validation
{
    public class ValidationFilter
    {
        public void ValidateRequest(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            Type validatorType = typeof(IValidator<>).MakeGenericType(requestDto.GetType());
            MethodInfo resolver = typeof(IHttpRequest).GetMethod("TryResolve")
                .MakeGenericMethod(validatorType);

            IValidator validator = (IValidator)resolver.Invoke(req, null);
            if (validator != null)
            {
                string ruleSet = req.HttpMethod;
                var validationResult = validator.Validate(new ValidationContext(requestDto, null, new MultiRuleSetValidatorSelector(ruleSet)));

                ResponseStatusTranslator translator = new ResponseStatusTranslator();
                ResponseStatus responseStatus = translator.Parse(validationResult.AsSerializable());
                res.WriteToResponse(req, responseStatus);
            }
        }
    }
}
