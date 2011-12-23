using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using FluentValidation;

namespace ServiceStack.ServiceInterface.Validation
{
    public class ValidationFilter
    {
        public void ValidateRequest<T>(IHttpRequest req, IHttpResponse res, T requestDto)
        {
            IValidator<T> validator = req.TryResolve<IValidator<T>>();
            if (validator != null)
            {
                var validationResult = validator.Validate(requestDto, ruleSet: req.HttpMethod);
                if (!validationResult.IsValid)
                    validationResult.Throw();
            }
        }
    }
}
