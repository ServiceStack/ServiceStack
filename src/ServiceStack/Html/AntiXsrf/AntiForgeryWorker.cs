// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if NET_4_0
using System.Diagnostics.Contracts;
#endif
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace ServiceStack.Html.AntiXsrf
{
    internal sealed class AntiForgeryWorker
    {
        private readonly IAntiForgeryConfig _config;
        private readonly IAntiForgeryTokenSerializer _serializer;
        private readonly ITokenStore _tokenStore;
        private readonly ITokenValidator _validator;

        internal AntiForgeryWorker(IAntiForgeryTokenSerializer serializer, IAntiForgeryConfig config, ITokenStore tokenStore, ITokenValidator validator)
        {
            _serializer = serializer;
            _config = config;
            _tokenStore = tokenStore;
            _validator = validator;
        }

        private void CheckSSLConfig(HttpContextBase httpContext)
        {
            if (_config.RequireSSL && !httpContext.Request.IsSecureConnection) {
                throw new InvalidOperationException(MvcResources.AntiForgeryWorker_RequireSSL);
            }
        }

        private AntiForgeryToken DeserializeToken(string serializedToken)
        {
            return (!String.IsNullOrEmpty(serializedToken))
                ? _serializer.Deserialize(serializedToken)
                : null;
        }

        private AntiForgeryToken DeserializeTokenNoThrow(string serializedToken)
        {
            try {
                return DeserializeToken(serializedToken);
            } catch {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        private static IIdentity ExtractIdentity(HttpContextBase httpContext)
        {
            if (httpContext != null) {
                IPrincipal user = httpContext.User;
                if (user != null) {
                    return user.Identity;
                }
            }
            return null;
        }

        private AntiForgeryToken GetCookieTokenNoThrow(HttpContextBase httpContext)
        {
            try {
                return _tokenStore.GetCookieToken(httpContext);
            } catch {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        // [ ENTRY POINT ]
        // Generates an anti-XSRF token pair for the current user. The return
        // value is the hidden input form element that should be rendered in
        // the <form>. This method has a side effect: it may set a response
        // cookie.
        public TagBuilder GetFormInputElement(HttpContextBase httpContext)
        {
            CheckSSLConfig(httpContext);

            AntiForgeryToken oldCookieToken = GetCookieTokenNoThrow(httpContext);
            AntiForgeryToken newCookieToken, formToken;
            GetTokens(httpContext, oldCookieToken, out newCookieToken, out formToken);

            if (newCookieToken != null) {
                // If a new cookie was generated, persist it.
                _tokenStore.SaveCookieToken(httpContext, newCookieToken);
            }

            // <input type="hidden" name="__AntiForgeryToken" value="..." />
            TagBuilder retVal = new TagBuilder("input");
            retVal.Attributes["type"] = "hidden";
            retVal.Attributes["name"] = _config.FormFieldName;
            retVal.Attributes["value"] = _serializer.Serialize(formToken);
            return retVal;
        }

        // [ ENTRY POINT ]
        // Generates a (cookie, form) serialized token pair for the current user.
        // The caller may specify an existing cookie value if one exists. If the
        // 'new cookie value' out param is non-null, the caller *must* persist
        // the new value to cookie storage since the original value was null or
        // invalid. This method is side-effect free.
        public void GetTokens(HttpContextBase httpContext, string serializedOldCookieToken, out string serializedNewCookieToken, out string serializedFormToken)
        {
            CheckSSLConfig(httpContext);

            AntiForgeryToken oldCookieToken = DeserializeTokenNoThrow(serializedOldCookieToken);
            AntiForgeryToken newCookieToken, formToken;
            GetTokens(httpContext, oldCookieToken, out newCookieToken, out formToken);

            serializedNewCookieToken = Serialize(newCookieToken);
            serializedFormToken = Serialize(formToken);
        }

        private void GetTokens(HttpContextBase httpContext, AntiForgeryToken oldCookieToken, out AntiForgeryToken newCookieToken, out AntiForgeryToken formToken)
        {
            newCookieToken = null;
            if (!_validator.IsCookieTokenValid(oldCookieToken)) {
                // Need to make sure we're always operating with a good cookie token.
                oldCookieToken = newCookieToken = _validator.GenerateCookieToken();
            }
#if NET_4_0
            Contract.Assert(_validator.IsCookieTokenValid(oldCookieToken));
#endif
            formToken = _validator.GenerateFormToken(httpContext, ExtractIdentity(httpContext), oldCookieToken);
        }

        private string Serialize(AntiForgeryToken token)
        {
            return (token != null) ? _serializer.Serialize(token) : null;
        }

        // [ ENTRY POINT ]
        // Given an HttpContext, validates that the anti-XSRF tokens contained
        // in the cookies & form are OK for this request.
        public void Validate(HttpContextBase httpContext)
        {
            CheckSSLConfig(httpContext);

            // Extract cookie & form tokens
            AntiForgeryToken cookieToken = _tokenStore.GetCookieToken(httpContext);
            AntiForgeryToken formToken = _tokenStore.GetFormToken(httpContext);

            // Validate
            _validator.ValidateTokens(httpContext, ExtractIdentity(httpContext), cookieToken, formToken);
        }

        // [ ENTRY POINT ]
        // Given the serialized string representations of a cookie & form token,
        // validates that the pair is OK for this request.
        public void Validate(HttpContextBase httpContext, string cookieToken, string formToken)
        {
            CheckSSLConfig(httpContext);

            // Extract cookie & form tokens
            AntiForgeryToken deserializedCookieToken = DeserializeToken(cookieToken);
            AntiForgeryToken deserializedFormToken = DeserializeToken(formToken);

            // Validate
            _validator.ValidateTokens(httpContext, ExtractIdentity(httpContext), deserializedCookieToken, deserializedFormToken);
        }
    }
}
