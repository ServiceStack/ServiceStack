using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;
using ServiceStack.FluentValidation;

namespace ServiceStack.ServiceInterface.Validation
{
    public static class ValidationHandler
    {
        public static void Init(IAppHost appHost)
        {
            var filter = new ValidationFilter();
            appHost.RequestFilters.Add(filter.ValidateRequest);
        }
    }
}
