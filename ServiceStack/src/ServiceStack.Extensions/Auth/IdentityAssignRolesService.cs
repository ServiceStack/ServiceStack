using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(AssignRoles))]
    public class IdentityAssignRolesService<TUser> : Service
        where TUser : IdentityUser
    {
        private readonly UserManager<TUser> userManager;
        public IdentityAssignRolesService(UserManager<TUser> userManager) => this.userManager = userManager;

        public async Task<object> PostAsync(AssignRoles request)
        {
            if (!Request.IsInProcessRequest())
                await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, RoleNames.Admin);
            
            if (string.IsNullOrEmpty(request.UserName))
                throw new ArgumentNullException(nameof(request.UserName));

            if (request.Roles == null || request.Roles.Count == 0)
                throw new ArgumentNullException(nameof(request.Roles));

            var user = await userManager.FindByEmailAsync(request.UserName).ConfigAwait();
            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

            await userManager.AddToRolesAsync(user, request.Roles);

            var roles = await userManager.GetRolesAsync(user).ConfigAwait();

            return new AssignRolesResponse
            {
                AllRoles = roles.ToList(),
            };
        }
    }
    
    [DefaultRequest(typeof(UnAssignRoles))]
    public class IdentityUnAssignRolesService<TUser> : Service
        where TUser : IdentityUser
    {
        private readonly UserManager<TUser> userManager;
        public IdentityUnAssignRolesService(UserManager<TUser> userManager) => this.userManager = userManager;

        public async Task<object> PostAsync(UnAssignRoles request)
        {
            if (!Request.IsInProcessRequest())
                await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, RoleNames.Admin);
            
            if (string.IsNullOrEmpty(request.UserName))
                throw new ArgumentNullException(nameof(request.UserName));

            if (request.Roles == null || request.Roles.Count == 0)
                throw new ArgumentNullException(nameof(request.Roles));

            var user = await userManager.FindByEmailAsync(request.UserName).ConfigAwait();
            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

            await userManager.RemoveFromRolesAsync(user, request.Roles);

            var roles = await userManager.GetRolesAsync(user).ConfigAwait();

            return new AssignRolesResponse
            {
                AllRoles = roles.ToList(),
            };
        }
    }    
    
}