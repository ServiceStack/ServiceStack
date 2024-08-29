#nullable enable

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IUserResolver
{
    Task<ClaimsPrincipal?> CreateClaimsPrincipalAsync(IRequest req, string userId, CancellationToken token=default);
    Task<IAuthSession?> CreateAuthSessionAsync(IRequest req, ClaimsPrincipal user, CancellationToken token = default);
}
