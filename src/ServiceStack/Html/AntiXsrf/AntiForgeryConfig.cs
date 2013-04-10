// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Web;

namespace ServiceStack.Html.AntiXsrf
{
    /// <summary>
    /// Provides programmatic configuration for the anti-forgery token system.
    /// </summary>
    public static class AntiForgeryConfig
    {
        internal const string AntiForgeryTokenFieldName = "__RequestVerificationToken";

        private static string _cookieName;
        private static string _uniqueClaimTypeIdentifier;

        /// <summary>
        /// Specifies an object that can provide additional data to put into all
        /// generated tokens and that can validate additional data in incoming
        /// tokens.
        /// </summary>
        public static IAntiForgeryAdditionalDataProvider AdditionalDataProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies the name of the cookie that is used by the anti-forgery
        /// system.
        /// </summary>
        /// <remarks>
        /// If an explicit name is not provided, the system will automatically
        /// generate a name.
        /// </remarks>
        public static string CookieName
        {
            get
            {
                if (_cookieName == null) {
                    _cookieName = GetAntiForgeryCookieName();
                }
                return _cookieName;
            }
            set
            {
                _cookieName = value;
            }
        }

        /// <summary>
        /// Specifies whether SSL is required for the anti-forgery system
        /// to operate. If this setting is 'true' and a non-SSL request
        /// comes into the system, all anti-forgery APIs will fail.
        /// </summary>
        public static bool RequireSsl
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies whether the anti-forgery system should skip checking
        /// for conditions that might indicate misuse of the system. Please
        /// use caution when setting this switch, as improper use could open
        /// security holes in the application.
        /// </summary>
        /// <remarks>
        /// Setting this switch will disable several checks, including:
        /// - Identity.IsAuthenticated = true without Identity.Name being set
        /// - special-casing claims-based identities
        /// </remarks>
        public static bool SuppressIdentityHeuristicChecks
        {
            get;
            set;
        }

        /// <summary>
        /// If claims-based authorization is in use, specifies the claim
        /// type from the identity that is used to uniquely identify the
        /// user. If this property is set, all claims-based identities
        /// <em>must</em> return unique values for this claim type.
        /// </summary>
        /// <remarks>
        /// If claims-based authorization is in use and this property has
        /// not been set, the anti-forgery system will automatically look
        /// for claim types "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        /// and "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider".
        /// </remarks>
        public static string UniqueClaimTypeIdentifier
        {
            get
            {
                return _uniqueClaimTypeIdentifier ?? String.Empty;
            }
            set
            {
                _uniqueClaimTypeIdentifier = value;
            }
        }

        private static string GetAntiForgeryCookieName()
        {
            return GetAntiForgeryCookieName(HttpRuntime.AppDomainAppVirtualPath);
        }

        // If the app path is provided, we're generating a cookie name rather than a field name, and the cookie names should
        // be unique so that a development server cookie and an IIS cookie - both running on localhost - don't stomp on
        // each other.
        internal static string GetAntiForgeryCookieName(string appPath)
        {
            if (String.IsNullOrEmpty(appPath) || appPath == "/") {
                return AntiForgeryTokenFieldName;
            } else {
                return AntiForgeryTokenFieldName + "_" + HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(appPath));
            }
        }
    }
}
