using System;
using System.Threading.Tasks;
using CheckIdentity.ServiceModel;
using Microsoft.AspNetCore.Identity;
using ServiceStack;

namespace CheckIdentity.ServiceInterface
{
    public class MyServices : Service
    {
        HelloResponse CreateResponse(object request, string name) =>
            new() { Result = $"{request.GetType().Name}, {name}!" };
        
        public object Any(Hello request) => CreateResponse(request, request.Name);
        public object Any(HelloAuth request) => CreateResponse(request, request.Name);
        public object Any(HelloRole request) => CreateResponse(request, request.Name);
    }
    
    [ValidateIsAdmin]
    public class CreateRole : IReturn<CreateRole>
    {
        public string Role { get; set; }
    }

    
    [ValidateIsAdmin]
    public class DeleteUser : IReturn<DeleteUser>
    {
        public string UserName { get; set; }
    }

    public class AdminServices : Service
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        public AdminServices(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<object> PostAsync(CreateRole request)
        {
            await roleManager.CreateAsync(new IdentityRole(request.Role));
            return request;
        }

        public async Task<object> PostAsync(DeleteUser request)
        {
            var userName = request.UserName ?? throw new ArgumentNullException(nameof(request.UserName));
            var user = await userManager.FindByEmailAsync(userName);
            if (user != null)
            {
                await userManager.DeleteAsync(user);
            }
            return request;
        }
    }

}
