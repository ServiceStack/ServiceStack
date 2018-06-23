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
            
            if (string.IsNullOrEmpty(request.UserName))
                throw new ArgumentNullException(nameof(request.UserName));

            var userAuth = AuthRepository.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            AuthRepository.AssignRoles(userAuth, request.Roles, request.Permissions);

            return new AssignRolesResponse
            {
                AllRoles = AuthRepository.GetRoles(userAuth).ToList(),
                AllPermissions = AuthRepository.GetPermissions(userAuth).ToList(),
            };
        }
    }
}