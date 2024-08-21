#nullable enable

using System.Security.Claims;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IUserResolver
{
    Task<ClaimsPrincipal?> CreateClaimsPrincipal(IRequest req, string userId);
    Task<IAuthSession?> CreateAuthSession(IRequest req, ClaimsPrincipal user);
}
