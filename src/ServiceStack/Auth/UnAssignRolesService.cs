using System;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(UnAssignRoles))]
    public class UnAssignRolesService : Service
    {
        public object Post(UnAssignRoles request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);

            request.UserName.ThrowIfNullOrEmpty();

            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
            using (authRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuthByUserName(request.UserName);
                if (userAuth == null)
                    throw HttpError.NotFound(request.UserName);

                authRepo.UnAssignRoles(userAuth, request.Roles, request.Permissions);

                return new UnAssignRolesResponse
                {
                    AllRoles = authRepo.GetRoles(userAuth).ToList(),
                    AllPermissions = authRepo.GetPermissions(userAuth).ToList(),
                };
            }
        }
    }
}