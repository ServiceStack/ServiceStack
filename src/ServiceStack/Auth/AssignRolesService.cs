using System;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(AssignRoles))]
    public class AssignRolesService : Service
    {
        public object Post(AssignRoles request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);

            request.UserName.ThrowIfNullOrEmpty();

            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
            using (authRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuthByUserName(request.UserName);
                if (userAuth == null)
                    throw HttpError.NotFound(request.UserName);

                authRepo.AssignRoles(userAuth, request.Roles, request.Permissions);

                return new AssignRolesResponse
                {
                    AllRoles = authRepo.GetRoles(userAuth).ToList(),
                    AllPermissions = authRepo.GetPermissions(userAuth).ToList(),
                };
            }
        }
    }
}