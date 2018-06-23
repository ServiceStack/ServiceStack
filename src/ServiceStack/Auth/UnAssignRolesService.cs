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

            if (string.IsNullOrEmpty(request.UserName))
                throw new ArgumentNullException(nameof(request.UserName));

            var userAuth = AuthRepository.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            AuthRepository.UnAssignRoles(userAuth, request.Roles, request.Permissions);

            return new UnAssignRolesResponse
            {
                AllRoles = AuthRepository.GetRoles(userAuth).ToList(),
                AllPermissions = AuthRepository.GetPermissions(userAuth).ToList(),
            };
        }
    }
}