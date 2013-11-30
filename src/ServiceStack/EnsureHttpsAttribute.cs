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

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (SkipIfDebugMode)
                return;

            if (!req.AbsoluteUri.StartsWith("https://"))
            {
                res.RedirectToUrl(req.AbsoluteUri.AsHttps());
                res.EndRequest();
            }
        }
    }
}