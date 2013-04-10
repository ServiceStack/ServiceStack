// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Reflection;
using System.Web;
using System.Web.Security;

namespace ServiceStack.Html.AntiXsrf
{
    // Interfaces with the System.Web.MachineKey static class using the 4.5 Protect / Unprotect methods.
    internal sealed class MachineKey45CryptoSystem : ICryptoSystem
    {
        private static readonly string[] _purposes = new string[] { "ServiceStack.Html.AntiXsrf.AntiForgeryToken.v1" };
        private static readonly MachineKey45CryptoSystem _singletonInstance = GetSingletonInstance();

        public static MachineKey45CryptoSystem Instance
        {
            get
            {
                return _singletonInstance;
            }
        }

        private static MachineKey45CryptoSystem GetSingletonInstance()
        {
            return new MachineKey45CryptoSystem();
        }

        public string Protect(byte[] data)
        {
#if NET_4_0
            byte[] rawProtectedBytes = MachineKey.Protect(data, _purposes);
            return HttpServerUtility.UrlTokenEncode(rawProtectedBytes);
#else
            return HttpServerUtility.UrlTokenEncode(data);
#endif
            }

        public byte[] Unprotect(string protectedData)
        {
#if NET_4_0
            byte[] rawProtectedBytes = HttpServerUtility.UrlTokenDecode(protectedData);
            return MachineKey.Unprotect(rawProtectedBytes, _purposes);
#else
            return HttpServerUtility.UrlTokenDecode(protectedData);
#endif
        }
    }
}
