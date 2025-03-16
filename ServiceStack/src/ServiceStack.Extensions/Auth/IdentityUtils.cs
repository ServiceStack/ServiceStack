#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Model;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class IdentityException(List<IdentityError> errors) : Exception(errors[0].Description), 
    IHasStatusCode, IResponseStatusConvertible
{
    public string Code { get; } = errors[0].Code;
    private List<IdentityError> Errors { get; } = errors;
    
    public int StatusCode => (int)HttpStatusCode.BadRequest;

    public ResponseStatus ToResponseStatus() => new()
    {
        ErrorCode = nameof(IdentityException),
        Message = Message,
        Errors = Errors.ConvertAll(x => new ResponseError
        {
            ErrorCode = x.Code, 
            Message = x.Description,
        }),
    };
}

public static class IdentityUtils
{
    public static void AssertSucceeded(this IdentityResult result)
    {
        if (!result.Succeeded)
            throw new IdentityException(result.Errors.ToList());
    }

    public static void AssertSucceededSync(this Task<IdentityResult> taskResult)
        => taskResult.GetAwaiter().GetResult().AssertSucceeded();

    public static TokenValidationParameters UseStandardJwtClaims(this TokenValidationParameters options)
    {
        options.NameClaimType = JwtClaimTypes.Name;
        options.RoleClaimType = JwtClaimTypes.Roles;
        return options;
    }

    public static List<string> GetClaimsPrincipalRoles(this IRequest req, ClaimsPrincipal principal)
    {
        var options = req.TryResolve<IOptions<IdentityOptions>>();
        var roleClaims = principal.Claims.Where(x => x.Type == options.Value.ClaimsIdentity.RoleClaimType).ToList();
        return roleClaims.Map(x => x.Value);
    }
}