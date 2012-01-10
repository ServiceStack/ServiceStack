using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has specific permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RequiredPermissionAttribute : RequestFilterAttribute
    {
        public List<string> RequiredPermissions { get; set; }

    	public RequiredPermissionAttribute(params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = ApplyTo.All;
        }

        public RequiredPermissionAttribute(ApplyTo applyTo, params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = applyTo;
        }

		public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
		{
			var session = req.GetSession();
			foreach (string requiredPermission in this.RequiredPermissions)
			{
				if (session == null || !session.HasPermission(requiredPermission))
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
