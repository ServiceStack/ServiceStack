using System;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Redirect to the https:// version of this url if not already.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class EnsureHttpsAttribute : RequestFilterAttribute
    {
        /// <summary>
        /// Don't redirect when in DebugMode
        /// </summary>
        public bool SkipIfDebugMode { get; set; }

        /// <summary>
        /// Don't redirect if the request was a forwarded request, e.g. from a Load Balancer
        /// </summary>
        public bool SkipIfXForwardedFor { get; set; }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (SkipIfDebugMode && HostContext.Config.DebugMode)
                return;

            if (!req.IsSecureConnection)
            {
                if (SkipIfXForwardedFor)
                {
                    var httpReq = req as IHttpRequest;
                    if (httpReq?.XForwardedFor != null)
                        return;
                }

                res.RedirectToUrl(req.AbsoluteUri.AsHttps());
                res.EndRequest();
            }
        }
    }
}