using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Indicates that the request dto, which is associated with this attribute,
	/// can only execute, if the user has specific roles.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method /*MVC Actions*/, Inherited = false, AllowMultiple = true)]
	public class RequiredRoleAttribute : RequestFilterAttribute
	{
		public List<string> RequiredRoles { get; set; }

		public RequiredRoleAttribute(params string[] roles)
		{
			this.RequiredRoles = roles.ToList();
			this.ApplyTo = ApplyTo.All;
		}

		public RequiredRoleAttribute(ApplyTo applyTo, params string[] roles)
		{
			this.RequiredRoles = roles.ToList();
			this.ApplyTo = applyTo;
		}
		
		public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
		{
			AuthenticateAttribute.AuthenticateIfBasicAuth(req, res);

			var session = req.GetSession();
			if (HasAllRoles(req, session)) return;

			res.StatusCode = (int)HttpStatusCode.Unauthorized;
			res.StatusDescription = "Invalid Role";
			res.Close();
		}

		public bool HasAllRoles(IHttpRequest req, IAuthSession session, IUserAuthRepository userAuthRepo=null)
		{
			if (HasAllRoles(session)) return true;

			if (userAuthRepo == null)
				userAuthRepo = req.TryResolve<IUserAuthRepository>();

			if (userAuthRepo == null) return false;

			var userAuth = userAuthRepo.GetUserAuth(session, null);
			session.UpdateSession(userAuth);

			if (HasAllRoles(session))
			{
				req.SaveSession(session);
				return true;
			}
			return false;
		}

		public bool HasAllRoles(IAuthSession session)
		{
			return this.RequiredRoles
				.All(requiredRole => session != null
					&& session.HasRole(requiredRole));
		}
	}

}
