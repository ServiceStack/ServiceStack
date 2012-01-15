using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has specific roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RequiredRoleAttribute : RequestFilterAttribute
    {
        public List<string> RequiredRoles { get; set; }

    	public RequiredRoleAttribute(params string[] roles)
        {
            this.RequiredRoles = roles.ToList();
            this.ApplyTo = ApplyTo.All;
        }

		public RequiredRoleAttribute(ApplyTo applyTo, params string[] permissions)
        {
            this.RequiredRoles = permissions.ToList();
            this.ApplyTo = applyTo;
        }

		public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
		{
			IAuthSession session = req.GetSession();
			foreach (string requiredRole in this.RequiredRoles)
			{
				if (session == null || !session.HasRole(requiredRole))
				{
					res.StatusCode = (int)HttpStatusCode.Unauthorized;
					res.StatusDescription = "Invalid Permissions";
					res.Close();
					return;
				}
			}
		}
	}

}
