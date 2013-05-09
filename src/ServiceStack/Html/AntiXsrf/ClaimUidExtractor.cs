// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;
using ServiceStack.Html.Claims;

namespace ServiceStack.Html.AntiXsrf
{
    internal sealed class ClaimUidExtractor : IClaimUidExtractor
    {
        internal const string NameIdentifierClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        internal const string IdentityProviderClaimType = "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider";

        private readonly ClaimsIdentityConverter _claimsIdentityConverter;
        private readonly IAntiForgeryConfig _config;

        internal ClaimUidExtractor(IAntiForgeryConfig config, ClaimsIdentityConverter claimsIdentityConverter)
        {
            _config = config;
            _claimsIdentityConverter = claimsIdentityConverter;
        }

        public BinaryBlob ExtractClaimUid(IIdentity identity)
        {
            if (identity == null || !identity.IsAuthenticated || _config.SuppressIdentityHeuristicChecks) {
                // Skip anonymous users
                // Skip when claims-based checks are disabled
                return null;
            }

            ClaimsIdentity claimsIdentity = _claimsIdentityConverter.TryConvert(identity);
            if (claimsIdentity == null) {
                // not a claims-based identity
                return null;
            }

            string[] uniqueIdentifierParameters = GetUniqueIdentifierParameters(claimsIdentity, _config.UniqueClaimTypeIdentifier);
            byte[] claimUidBytes = CryptoUtil.ComputeSHA256(uniqueIdentifierParameters);
            return new BinaryBlob(256, claimUidBytes);
        }

        internal static string[] GetUniqueIdentifierParameters(ClaimsIdentity claimsIdentity, string uniqueClaimTypeIdentifier)
        {
            var claims = claimsIdentity.GetClaims();

            // The application developer might not want to use our default behavior
            // and instead might want us to use a claim he knows is unique within
            // the security realm of his application. (Perhaps he has crafted this
            // claim himself.)
            if (!String.IsNullOrEmpty(uniqueClaimTypeIdentifier)) {
                Claim matchingClaim = claims.SingleOrDefault(claim => String.Equals(uniqueClaimTypeIdentifier, claim.ClaimType, StringComparison.Ordinal));
                if (matchingClaim == null || String.IsNullOrEmpty(matchingClaim.Value)) {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, MvcResources.ClaimUidExtractor_ClaimNotPresent, uniqueClaimTypeIdentifier));
                }

                return new string[]
                {
                    uniqueClaimTypeIdentifier,
                    matchingClaim.Value
                };
            }

            // By default, we look for 'nameIdentifier' and 'identityProvider' claims.
            // For a correctly configured ACS consumer, this tuple will uniquely
            // identify a user of the application. We assume that a well-behaved
            // identity provider will never assign the same name identifier to multiple
            // users within its security realm, and we assume that ACS has been
            // configured so that each identity provider has a unique 'identityProvider'
            // claim.
            Claim nameIdentifierClaim = claims.SingleOrDefault(claim => String.Equals(NameIdentifierClaimType, claim.ClaimType, StringComparison.Ordinal));
            Claim identityProviderClaim = claims.SingleOrDefault(claim => String.Equals(IdentityProviderClaimType, claim.ClaimType, StringComparison.Ordinal));
            if (nameIdentifierClaim == null || String.IsNullOrEmpty(nameIdentifierClaim.Value)
                || identityProviderClaim == null || String.IsNullOrEmpty(identityProviderClaim.Value)) {
                throw new InvalidOperationException(MvcResources.ClaimUidExtractor_DefaultClaimsNotPresent);
            }

            return new string[]
            {
                NameIdentifierClaimType,
                nameIdentifierClaim.Value,
                IdentityProviderClaimType,
                identityProviderClaim.Value
            };
        }
    }
}
