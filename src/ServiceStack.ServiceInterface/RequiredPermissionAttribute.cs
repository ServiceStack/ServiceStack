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
			if (HasAllPermissions(session)) return;

			var userAuthRepo = req.TryResolve<IUserAuthRepository>();
			var userAuth = userAuthRepo.GetUserAuth(session, null);
			session.UpdateSession(userAuth);
			
			if (HasAllPermissions(session))
			{
				req.SaveSession(session);
				return;
			}

			res.StatusCode = (int)HttpStatusCode.Unauthorized;
			res.StatusDescription = "Invalid Permissions";
			res.Close();
		}

    	private bool HasAllPermissions(IAuthSession session)
    	{
    		return this.RequiredPermissions
				.All(requiredPermission => session != null 
					&& session.HasPermission(requiredPermission));
    	}
    }

}
