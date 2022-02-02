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
            var webRequest = WebRequest.CreateHttp(requestUri);

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

        private static readonly TaskFactory SyncTaskFactory = new TaskFactory(CancellationToken.None,
            TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
        public static void RunSync(Func<Task> task) => SyncTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
        public static TResult RunSync<TResult>(Func<Task<TResult>> task) => SyncTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
    }
}