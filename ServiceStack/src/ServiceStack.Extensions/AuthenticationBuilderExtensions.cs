using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ServiceStack;

public static class AuthenticationBuilderExtensions
{
    public static IdentityCookiesBuilder DisableRedirectsForApis(this IdentityCookiesBuilder builder)
    {
        builder.ApplicationCookie!.Configure(configure =>
        {
            configure.Events.OnRedirectToLogin += ctx =>
            {
                if ((ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Headers.Accept.ToString().Contains(MimeTypes.Json, StringComparison.OrdinalIgnoreCase)) 
                    && ctx.Response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status302Found)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    if (ctx.HttpContext.Items.TryGetValue(Keywords.ResponseStatus, out var oStatus) && oStatus is ResponseStatus status)
                    {
                        var dto = new ErrorResponse { ResponseStatus = status };
                        ctx.Response.WriteAsJsonAsync(dto);
                    }
                }
                return Task.CompletedTask;
            };
            configure.Events.OnRedirectToAccessDenied += ctx =>
            {
                if ((ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Headers.Accept.ToString().Contains(MimeTypes.Json, StringComparison.OrdinalIgnoreCase)) 
                    && ctx.Response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status302Found)
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    if (ctx.HttpContext.Items.TryGetValue(Keywords.ResponseStatus, out var oStatus) && oStatus is ResponseStatus status)
                    {
                        var dto = new ErrorResponse { ResponseStatus = status };
                        ctx.Response.WriteAsJsonAsync(dto);
                    }
                }
                return Task.CompletedTask;
            };
        });
        return builder;
    }
}