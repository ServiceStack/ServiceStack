// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace ServiceStack.Html.AntiXsrf
{
    internal sealed class AntiForgeryConfigWrapper : IAntiForgeryConfig
    {
        public IAntiForgeryAdditionalDataProvider AdditionalDataProvider
        {
            get
            {
                return AntiForgeryConfig.AdditionalDataProvider;
            }
        }

        public string CookieName
        {
            get { return AntiForgeryConfig.CookieName; }
        }

        public string FormFieldName
        {
            get { return AntiForgeryConfig.AntiForgeryTokenFieldName; }
        }

        public bool RequireSSL
        {
            get { return AntiForgeryConfig.RequireSsl; }
        }

        public bool SuppressIdentityHeuristicChecks
        {
            get { return AntiForgeryConfig.SuppressIdentityHeuristicChecks; }
        }

        public string UniqueClaimTypeIdentifier
        {
            get { return AntiForgeryConfig.UniqueClaimTypeIdentifier; }
        }
    }
}
