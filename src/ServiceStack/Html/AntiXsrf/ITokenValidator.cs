// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Security.Principal;
using System.Web;

namespace ServiceStack.Html.AntiXsrf
{
    // Provides an abstraction around something that can validate anti-XSRF tokens
    internal interface ITokenValidator
    {
        // Generates a new random cookie token.
        AntiForgeryToken GenerateCookieToken();

        // Given a cookie token, generates a corresponding form token.
        // The incoming cookie token must be valid.
        AntiForgeryToken GenerateFormToken(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken cookieToken);

        // Determines whether an existing cookie token is valid (well-formed).
        // If it is not, the caller must call GenerateCookieToken() before calling GenerateFormToken().
        bool IsCookieTokenValid(AntiForgeryToken cookieToken);

        // Validates a (cookie, form) token pair.
        void ValidateTokens(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken cookieToken, AntiForgeryToken formToken);
    }
}
