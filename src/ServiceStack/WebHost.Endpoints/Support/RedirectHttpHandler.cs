using System;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class RedirectHttpHandler
		: IServiceStackHttpHandler, IHttpHandler
	{
		public string RelativeUrl { get; set; }

		public string AbsoluteUrl { get; set; }

		/// <summary>
		/// Non ASP.NET requests
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="operationName"></param>
		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
			if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
				throw new ArgumentNullException("RelativeUrl or AbsoluteUrl");

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

            response.EndHttpRequest(skipClose:true);
        }

        /// <summary>
        /// ASP.NET requests
        /// </summary>
        /// <param name="context"></param>
		public void ProcessRequest(HttpContext context)
		{
        	var request = context.Request;
			var response = context.Response;

			if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
				throw new ArgumentNullException("RelativeUrl or AbsoluteUrl");

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

            response.EndHttpRequest(closeOutputStream:true);
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}