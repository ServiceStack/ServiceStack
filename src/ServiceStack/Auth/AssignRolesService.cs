using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(AssignRoles))]
    public class AssignRolesService : Service
    {
        public IAuthRepository UserAuthRepo { get; set; }

        public object Post(AssignRoles request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);

            request.UserName.ThrowIfNullOrEmpty();

            var userAuth = UserAuthRepo.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            UserAuthRepo.AssignRoles(userAuth, request.Roles, request.Permissions);

            return new AssignRolesResponse {
                AllRoles = UserAuthRepo.GetRoles(userAuth).ToList(),
                AllPermissions = UserAuthRepo.GetPermissions(userAuth).ToList(),
            };
        }
    }
}