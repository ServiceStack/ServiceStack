using System;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Base class to create response filter attributes only for specific HTTP methods (GET, POST...)
    /// </summary>
    public abstract class ResponseFilterAttribute : Attribute, IHasResponseFilter
    {
        public int Priority { get; set; }

        public ApplyTo ApplyTo { get; set; }

        public ResponseFilterAttribute()
        {
            ApplyTo = ApplyTo.All;
        }

        /// <summary>
        /// Creates a new <see cref="ResponseFilterAttribute"/>
        /// </summary>
        /// <param name="applyTo">Defines when the filter should be executed</param>
        public ResponseFilterAttribute(ApplyTo applyTo)
        {
            ApplyTo = applyTo;
        }

        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object response)
        {
            ApplyTo httpMethod = req.HttpMethodAsApplyTo();
            if (ApplyTo.Has(httpMethod))
                this.Execute(req, res, response);
        }

        /// <summary>
        /// This method is only executed if the HTTP method matches the <see cref="ApplyTo"/> property.
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        /// <param name="requestDto">The response DTO</param>
        public abstract void Execute(IHttpRequest req, IHttpResponse res, object responseDto);
        
        /// <summary>
        /// Create a ShallowCopy of this instance.
        /// </summary>
        /// <returns></returns>
        public virtual IHasResponseFilter Copy()
        {
            return (IHasResponseFilter)this.MemberwiseClone();
        }
    }
}
