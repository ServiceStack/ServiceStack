﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Base class to create request filter attributes only for specific HTTP methods (GET, POST...)
    /// </summary>
    public abstract class RequestFilterAttribute : Attribute, IHasRequestFilter
    {
        public ApplyTo ApplyTo { get; set; }

        public RequestFilterAttribute()
        {
            ApplyTo = ApplyTo.All;
        }

        /// <summary>
        /// Creates a new <see cref="RequestFilterAttribute"/>
        /// </summary>
        /// <param name="applyTo">Defines when the filter should be executed</param>
        public RequestFilterAttribute(ApplyTo applyTo)
        {
            ApplyTo = applyTo;
        }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            ApplyTo httpMethod = req.HttpMethodAsApplyTo();
            if (ApplyTo.Has(httpMethod))
                this.Execute(req, res, requestDto);
        }

        /// <summary>
        /// This method is only executed if the HTTP method matches the <see cref="ApplyTo"/> property.
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public abstract void Execute(IHttpRequest req, IHttpResponse res, object requestDto);
    }
}
