#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ServiceStack.Web;

/// <summary>
/// Configuration options for Sec-Fetch-Site CSRF middleware.
/// </summary>
public class SecFetchSiteCsrfOptions
{
    /// <summary>
    /// The verification strategy to use.
    /// </summary>
    public CsrfVerificationStrategy VerificationStrategy { get; set; } = CsrfVerificationStrategy.HeaderOnly;

    /// <summary>
    /// Trusted origins that are allowed to make cross-site requests.
    /// Useful for OAuth/SSO callbacks and third-party integrations.
    /// </summary>
    public HashSet<string> TrustedOrigins { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Header name containing the anti-forgery token for fallback validation.
    /// Defaults to "X-CSRF-TOKEN".
    /// </summary>
    public string AntiforgeryTokenHeader { get; set; } = "X-CSRF-TOKEN";

    /// <summary>
    /// Form field name containing the anti-forgery token for fallback validation.
    /// Defaults to "__RequestVerificationToken".
    /// </summary>
    public string AntiforgeryTokenFormField { get; set; } = "__RequestVerificationToken";

    /// <summary>
    /// Custom predicate to determine if a request should bypass CSRF checks.
    /// Useful for APIs that don't require protection (e.g., public endpoints).
    /// </summary>
    public Func<HttpContext, bool> BypassPredicate { get; set; } = _ => false;

    /// <summary>
    /// Custom handler for when a CSRF violation is detected.
    /// By default, returns 403 Forbidden.
    /// </summary>
    public Func<HttpContext, Task> OnCsrfViolation { get; set; } = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
}

/// <summary>
/// Defines the CSRF verification strategy.
/// </summary>
public enum CsrfVerificationStrategy
{
    /// <summary>
    /// Only verify using Sec-Fetch-Site header. Requests without the header
    /// or with "none" value are rejected.
    /// </summary>
    HeaderOnly,

    /// <summary>
    /// Verify using Sec-Fetch-Site header, falling back to traditional
    /// anti-forgery token validation when the header is missing or "none".
    /// </summary>
    HeaderWithTokenFallback
}

/// <summary>
/// Middleware that provides CSRF protection using the Sec-Fetch-Site header.
/// This is a modern, browser-based approach that doesn't require token management
/// for same-origin/same-site requests.
/// </summary>
public class SecFetchSiteCsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecFetchSiteCsrfOptions _options;
    private readonly IAntiforgery _antiforgery;

    private static readonly HashSet<string> SafeSiteValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "same-origin",
        "same-site"
    };

    public SecFetchSiteCsrfMiddleware(
        RequestDelegate next,
        IOptions<SecFetchSiteCsrfOptions> options,
        IAntiforgery antiforgery)
    {
        _next = next;
        _options = options.Value;
        _antiforgery = antiforgery;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-GET/HEAD requests that don't require CSRF protection
        // (Browsers don't send cross-site POST/PUT/DELETE for user-initiated navigation)
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            await _next(context);
            return;
        }

        // Check custom bypass predicate
        if (_options.BypassPredicate(context))
        {
            await _next(context);
            return;
        }

        // Check if request has Sec-Fetch-Site header
        if (!context.Request.Headers.TryGetValue("Sec-Fetch-Site", out var secFetchSiteValues))
        {
            // No Sec-Fetch-Site header - could be non-browser client
            // Fall back to token validation if strategy allows
            if (_options.VerificationStrategy == CsrfVerificationStrategy.HeaderOnly)
            {
                await _options.OnCsrfViolation(context);
                return;
            }

            await ValidateTokenAndContinue(context);
            return;
        }

        var secFetchSite = secFetchSiteValues.ToString().ToLowerInvariant();

        if (secFetchSite == "none")
        {
            // Browser-initiated request from address bar - treat like missing header
            if (_options.VerificationStrategy == CsrfVerificationStrategy.HeaderOnly)
            {
                await _options.OnCsrfViolation(context);
                return;
            }

            await ValidateTokenAndContinue(context);
            return;
        }

        if (SafeSiteValues.Contains(secFetchSite))
        {
            // Same-origin or same-site - allowed
            await _next(context);
            return;
        }

        // Cross-site request - check trusted origins
        if (IsCrossSiteRequestAllowed(context))
        {
            await _next(context);
            return;
        }

        // Unknown/untrusted cross-site request - reject
        await _options.OnCsrfViolation(context);
    }

    private bool IsCrossSiteRequestAllowed(HttpContext context)
    {
        // Cross-site but might be from trusted origin
        var origin = context.Request.Headers.Origin.ToString();

        if (string.IsNullOrEmpty(origin))
            return false;

        // Check if origin is in trusted list
        return _options.TrustedOrigins.Contains(origin);
    }

    private async Task ValidateTokenAndContinue(HttpContext context)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(context);
            await _next(context);
        }
        catch (AntiforgeryValidationException)
        {
            await _options.OnCsrfViolation(context);
        }
    }
}

/// <summary>
/// Extension methods for registering Sec-Fetch-Site CSRF middleware.
/// </summary>
public static class SecFetchSiteCsrfMiddlewareExtensions
{
    /// <summary>
    /// Adds Sec-Fetch-Site CSRF protection middleware to the application pipeline.
    /// Requires .NET 10.0 or later.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="configureOptions">Optional configuration options.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseSecFetchSiteCsrf(
        this IApplicationBuilder builder,
        Action<SecFetchSiteCsrfOptions>? configureOptions = null)
    {
        var options = new SecFetchSiteCsrfOptions();
        configureOptions?.Invoke(options);

        return builder.UseMiddleware<SecFetchSiteCsrfMiddleware>(Options.Create(options));
    }

    /// <summary>
    /// Adds Sec-Fetch-Site CSRF protection middleware to the application pipeline
    /// with the specified options.
    /// Requires .NET 10.0 or later.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="options">The configuration options.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseSecFetchSiteCsrf(
        this IApplicationBuilder builder,
        SecFetchSiteCsrfOptions options)
    {
        return builder.UseMiddleware<SecFetchSiteCsrfMiddleware>(Options.Create(options));
    }
}
#endif
