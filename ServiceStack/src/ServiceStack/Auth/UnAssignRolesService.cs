using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth;

[DefaultRequest(typeof(UnAssignRoles))]
public class UnAssignRolesService : Service
{
    public async Task<object> Post(UnAssignRoles request)
    {
        if (!Request.IsInProcessRequest())
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, RoleNames.Admin);

        if (string.IsNullOrEmpty(request.UserName))
            throw new ArgumentNullException(nameof(request.UserName));

        var userAuth = await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.UserName).ConfigAwait();
        if (userAuth == null)
            throw HttpError.NotFound(request.UserName);

        await AuthRepositoryAsync.UnAssignRolesAsync(userAuth, request.Roles, request.Permissions).ConfigAwait();

        return new UnAssignRolesResponse
        {
            AllRoles = (await AuthRepositoryAsync.GetRolesAsync(userAuth).ConfigAwait()).ToList(),
            AllPermissions = (await AuthRepositoryAsync.GetPermissionsAsync(userAuth).ConfigAwait()).ToList(),
        };
    }
}