using System;
using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class RedirectHttpHandler : HttpAsyncTaskHandler
    {
        public RedirectHttpHandler()
        {
            this.RequestName = GetType().Name;
        }

        public string RelativeUrl { get; set; }

        public string AbsoluteUrl { get; set; }

        /// <summary>
        /// Non ASP.NET requests
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="operationName"></param>
        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
                throw new ArgumentException("RelativeUrl and AbsoluteUrl is Required");

            if (!string.IsNullOrEmpty(AbsoluteUrl))
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, this.AbsoluteUrl);
            }
            else
            {
                var absoluteUrl = request.GetApplicationUrl();
                if (!string.IsNullOrEmpty(RelativeUrl))
                {
                    if (this.RelativeUrl.StartsWith("/"))
                        absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl);
                    else if (this.RelativeUrl.StartsWith("~/"))
                        absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl.Replace("~/", ""));
                    else
                        absoluteUrl = request.AbsoluteUri.CombineWith(this.RelativeUrl);
                }
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, absoluteUrl);
            }

            response.EndHttpHandlerRequest(skipClose: true);
        }

#if !NETSTANDARD1_6
        /// <summary>
        /// ASP.NET requests
        /// </summary>
        /// <param name="context"></param>
        public override void ProcessRequest(HttpContextBase context)
        {
            var request = context.Request;
            var response = context.Response;

            if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
                throw new ArgumentException("RelativeUrl and AbsoluteUrl is Required");

            if (!string.IsNullOrEmpty(AbsoluteUrl))
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, this.AbsoluteUrl);
            }
            else
            {
                var absoluteUrl = request.GetApplicationUrl();
                if (!string.IsNullOrEmpty(RelativeUrl))
                {
                    if (this.RelativeUrl.StartsWith("/"))
                        absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl);
                    else if (this.RelativeUrl.StartsWith("~/"))
                        absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl.Replace("~/", ""));
                    else
                        absoluteUrl = request.Url.AbsoluteUri.CombineWith(this.RelativeUrl);
                }
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, absoluteUrl);
            }

            context.EndHttpHandlerRequest(closeOutputStream: true);
        }
#endif

    }
}