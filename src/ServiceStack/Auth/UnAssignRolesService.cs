using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class UnAssignRoles : IReturn<UnAssignRolesResponse>
    {
        public UnAssignRoles()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        public string UserName { get; set; }

        public List<string> Permissions { get; set; }

        public List<string> Roles { get; set; }
    }

    public class UnAssignRolesResponse : IHasResponseStatus
    {
        public UnAssignRolesResponse()
        {
            this.AllRoles = new List<string>();
        }

        public List<string> AllRoles { get; set; }

        public List<string> AllPermissions { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [RequiredRole(RoleNames.Admin)]
    [DefaultRequest(typeof(UnAssignRoles))]
    public class UnAssignRolesService : Service
    {
        public IAuthRepository UserAuthRepo { get; set; }

        public object Post(UnAssignRoles request)
        {
            request.UserName.ThrowIfNullOrEmpty();

            var userAuth = UserAuthRepo.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            if (!request.Roles.IsEmpty())
            {
                request.Roles.ForEach(x => userAuth.Roles.Remove(x));
            }
            if (!request.Permissions.IsEmpty())
            {
                request.Permissions.ForEach(x => userAuth.Permissions.Remove(x));
            }

            UserAuthRepo.SaveUserAuth(userAuth);

            return new UnAssignRolesResponse {
                AllRoles = userAuth.Roles,
                AllPermissions = userAuth.Permissions,
            };
        }
    }
}