using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Uses built in ASP.NET HttpContext.Current.IsAuthenticated to determine if user is authorized based on roles given.
    /// Essentially integrates to be exactly like ASP.Net MVC [AuthorizeAttibute], except authorizing service stack calls instead of MVC controller/actions
    /// http://msdn.microsoft.com/en-us/library/system.web.mvc.authorizeattribute(v=vs.108).aspx
    /// </summary>
    public class AuthorizeAttribute : RequestFilterAttribute
    {
        private string _roles;
        private string[] _rolesSplit = new string[0];

        public string Roles
        {
            get { return _roles ?? String.Empty; }
            set
            {
                _roles = value;
                _rolesSplit = SplitString(value);
            }
        }

        public AuthorizeAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
            this.Priority = (int)RequestFilterPriority.Authenticate;
        }

        public AuthorizeAttribute()
            : this(ApplyTo.All) { }


        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (!InternalAuthorize())
            {
                res.StatusCode = (int)HttpStatusCode.Unauthorized;
                res.EndServiceStackRequest();
            }
        }

        private bool InternalAuthorize()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                var user = context.User;
                if (user != null)
                {
                    if (!user.Identity.IsAuthenticated)
                        return false;
                    if (_rolesSplit.Length > 0 && !_rolesSplit.Any(user.IsInRole))
                        return false;
                    return true;
                }
            }
            return false;
        }

        private static string[] SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }

    }
}
