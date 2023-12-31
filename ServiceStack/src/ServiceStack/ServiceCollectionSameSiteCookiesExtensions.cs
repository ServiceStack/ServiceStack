#if NET6_0_OR_GREATER
#nullable enable
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ServiceStack;

/// <summary>
/// From: https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/
/// </summary>
public static class ServiceCollectionSameSiteCookiesExtensions
{
    /// <summary>
    /// Configures a cookie policy to properly set the SameSite attribute
    /// for Browsers that handle unknown values as Strict. Ensure that you
    /// add the <seealso cref="Microsoft.AspNetCore.CookiePolicy.CookiePolicyMiddleware" />
    /// into the pipeline before sending any cookies!
    /// </summary>
    public static IServiceCollection ConfigureNonBreakingSameSiteCookies(this IServiceCollection services,
        IWebHostEnvironment env)
    {
        services.ConfigureNonBreakingSameSiteCookies(options => {
            options.Secure = env.IsDevelopment()
                ? CookieSecurePolicy.None
                : CookieSecurePolicy.Always;
        });
        return services;
    }

    /// <summary>
    /// Configures a cookie policy to properly set the SameSite attribute
    /// for Browsers that handle unknown values as Strict. Ensure that you
    /// add the <seealso cref="Microsoft.AspNetCore.CookiePolicy.CookiePolicyMiddleware" />
    /// into the pipeline before sending any cookies!
    /// </summary>
    public static IServiceCollection ConfigureNonBreakingSameSiteCookies(this IServiceCollection services, 
        Action<CookiePolicyOptions>? configure = null)
    {
        services.Configure<CookiePolicyOptions>(options => {
            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            options.OnAppendCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            options.OnDeleteCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            
            options.HttpOnly = HttpOnlyPolicy.Always;
            configure?.Invoke(options);
        });

        return services;
    }

    private static void CheckSameSite(HttpContext httpContext, CookieOptions options)
    {
        if (options.SameSite == SameSiteMode.None)
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            if (!httpContext.Request.IsHttps || DisallowsSameSiteNone(userAgent))
            {
                options.SameSite = SameSiteMode.Unspecified;
            }
        }
    }

    private static bool DisallowsSameSiteNone(string userAgent)
    {
        // Cover all iOS based browsers here. This includes:
        //   - Safari on iOS 12 for iPhone, iPod Touch, iPad
        //   - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
        //   - Chrome on iOS 12 for iPhone, iPod Touch, iPad
        // All of which are broken by SameSite=None, because they use the iOS networking stack.
        if (userAgent.Contains("CPU iPhone OS 12")
            || userAgent.Contains("iPad; CPU OS 12"))
            return true;

        // Cover Mac OS X based browsers that use the Mac OS networking stack.
        // This includes:
        //   - Safari on Mac OS X.
        // This does not include:
        //   - Chrome on Mac OS X
        if (userAgent.Contains("Safari")
            && userAgent.Contains("Macintosh; Intel Mac OS X 10_14")
            && userAgent.Contains("Version/"))
            return true;

        // Cover Chrome 50-69, because some versions are broken by SameSite=None
        // and none in this range require it.
        // Note: this covers some pre-Chromium Edge versions,
        // but pre-Chromium Edge does not require SameSite=None.
        if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            return true;

        return false;
    }

    public static IHttpClientBuilder AddHttpUtilsClient(this IServiceCollection services)
    {
        HostContext.ConfigureAppHost(appHost => HttpUtils.CreateClient = () => 
            appHost.TryResolve<IHttpClientFactory>().CreateClient(nameof(HttpUtils)));
        return services.AddHttpClient(nameof(HttpUtils));
    }
    
}
#endif