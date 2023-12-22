using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth;

[DefaultRequest(typeof(AssignRoles))]
public class AssignRolesService : Service
{
    public async Task<object> Post(AssignRoles request)
    {
        if (!Request.IsInProcessRequest())
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, RoleNames.Admin);
            
        if (string.IsNullOrEmpty(request.UserName))
            throw new ArgumentNullException(nameof(request.UserName));

        var userAuth = await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.UserName).ConfigAwait();
        if (userAuth == null)
            throw HttpError.NotFound(request.UserName);

        await AuthRepositoryAsync.AssignRolesAsync(userAuth, request.Roles, request.Permissions).ConfigAwait();

        return new AssignRolesResponse
        {
            AllRoles = (await AuthRepositoryAsync.GetRolesAsync(userAuth).ConfigAwait()).ToList(),
            AllPermissions = (await AuthRepositoryAsync.GetPermissionsAsync(userAuth).ConfigAwait()).ToList(),
        };
    }
}