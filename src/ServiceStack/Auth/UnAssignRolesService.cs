using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(UnAssignRoles))]
    public class UnAssignRolesService : Service
    {
        public IAuthRepository UserAuthRepo { get; set; }

        public object Post(UnAssignRoles request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);

            request.UserName.ThrowIfNullOrEmpty();

            var userAuth = UserAuthRepo.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            UserAuthRepo.UnAssignRoles(userAuth, request.Roles, request.Permissions);

            return new UnAssignRolesResponse {
                AllRoles = UserAuthRepo.GetRoles(userAuth).ToList(),
                AllPermissions = UserAuthRepo.GetPermissions(userAuth).ToList(),
            };
        }
    }
}