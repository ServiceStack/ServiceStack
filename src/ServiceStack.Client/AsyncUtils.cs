// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

/*
 * Keep as much platform specific stuff here
 */

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface ITimer : IDisposable
    {
        void Cancel();
    }

    public delegate void ProgressDelegate(long done, long total);

    internal static class AsyncUtils
    {
        internal static HttpWebRequest CreateHttpWebRequest(this AsyncServiceClient client, string requestUri)
        {
            var webRequest = PclExport.Instance.CreateWebRequest(requestUri, 
                emulateHttpViaPost:client.EmulateHttpViaPost);

            PclExport.Instance.Config(webRequest);

            if (client.StoreCookies)
            {
                PclExportClient.Instance.SetCookieContainer(webRequest, client);
            }

            if (!client.DisableAutoCompression)
            {
                PclExport.Instance.AddCompression(webRequest);
            }

            return webRequest;
        }
    }
}