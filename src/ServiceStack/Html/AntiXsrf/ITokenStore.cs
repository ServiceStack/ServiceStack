// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web;

namespace ServiceStack.Html.AntiXsrf
{
    // Provides an abstraction around how tokens are persisted and retrieved for a request
    internal interface ITokenStore
    {
        AntiForgeryToken GetCookieToken(HttpContextBase httpContext);
        AntiForgeryToken GetFormToken(HttpContextBase httpContext);
        void SaveCookieToken(HttpContextBase httpContext, AntiForgeryToken token);
    }
}
