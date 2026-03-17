using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceModel;
using Org.BouncyCastle.Ocsp;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;

namespace MyApp.ServiceInterface;

public class AccountServices(IIdentityAuthContextManager userManager, JsonApiClient client) : Service
{
    public async Task<object> Any(GetAccount request)
    {
        var apiKey = Request.GetApiKey();
        var userId = Request.GetRequiredUserId();
        userId = apiKey.UserAuthId!;
        
        var user = await userManager.CreateClaimsPrincipalAsync(userId, Request);
        
        if (user.HasRole(RoleNames.Admin))
        {
            throw new HttpError(403, "Access Denied");
        }
        
        // user.HasRole(RoleNames.Admin);
        return new GetAccountResponse
        {
            UserId = userId,
            Username = user.GetUserName(),
            DisplayName = user.GetDisplayName(),
            Email = user.GetEmail(),
            Roles = user.GetRoles()
        }; 
    }

    public async Task<object> Any(GetKey request)
    {
        var api = await client.ApiAsync(new GetPublicKey());
        return api.Response;
    } 
}
