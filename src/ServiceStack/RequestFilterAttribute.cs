using System;
using ServiceStack.Web;

namespace ServiceStack
{
    public enum RequestFilterPriority : int
    {
        Authenticate = -100,
        RequiredRole = -90,
        RequiredPermission = -80,
    }

    /// <summary>
    /// Base class to create request filter attributes only for specific HTTP methods (GET, POST...)
    /// </summary>
    public abstract class RequestFilterAttribute : AttributeBase, IHasRequestFilter
    {
        public int Priority { get; set; }

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

        public void RequestFilter(IRequest req, IResponse res, object requestDto)
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
        public abstract void Execute(IRequest req, IResponse res, object requestDto);

        /// <summary>
        /// Create a ShallowCopy of this instance.
        /// </summary>
        /// <returns></returns>
        public virtual IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)this.MemberwiseClone();
        }
    }
}
