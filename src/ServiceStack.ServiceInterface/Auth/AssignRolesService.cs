using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceInterface.Auth
{
    public class AssignRoles : IReturn<AssignRolesResponse>
    {
        public AssignRoles()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        public string UserName { get; set; }

        public List<string> Permissions { get; set; }

        public List<string> Roles { get; set; }
    }

    public class AssignRolesResponse : IHasResponseStatus
    {
        public AssignRolesResponse()
        {
            this.AllRoles = new List<string>();
            this.AllPermissions = new List<string>();
        }

        public List<string> AllRoles { get; set; }

        public List<string> AllPermissions { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [RequiredRole(RoleNames.Admin)]
    [DefaultRequest(typeof(AssignRoles))]
    public class AssignRolesService : Service
    {
        public IUserAuthRepository UserAuthRepo { get; set; }

        public object Post(AssignRoles request)
        {
            request.UserName.ThrowIfNullOrEmpty();

            var userAuth = UserAuthRepo.GetUserAuthByUserName(request.UserName);
            if (userAuth == null)
                throw HttpError.NotFound(request.UserName);

            if (!request.Roles.IsEmpty())
            {
                foreach (var missingRole in request.Roles.Where(x => !userAuth.Roles.Contains(x)))
                {
                    userAuth.Roles.Add(missingRole);
                }
            }
            if (!request.Permissions.IsEmpty())
            {
                foreach (var missingPermission in request.Permissions.Where(x => !userAuth.Permissions.Contains(x)))
                {
                    userAuth.Permissions.Add(missingPermission);
                }
            }

            UserAuthRepo.SaveUserAuth(userAuth);

            return new AssignRolesResponse {
                AllRoles = userAuth.Roles,
                AllPermissions = userAuth.Permissions,
            };
        }
    }
}