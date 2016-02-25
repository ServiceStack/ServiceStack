//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if ANDROID
using System;
using System.Collections.Specialized;
using System.Threading;
using Android.OS;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack
{
    public class AndroidPclExportClient : PclExportClient
    {
        public static AndroidPclExportClient Provider = new AndroidPclExportClient();

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
        }

        public Handler UiHandler;
        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new AndroidPclExportClient()));
            AndroidPclExport.Configure();

            Provider.UiHandler = new Handler(Looper.MainLooper);

            return Provider;
        }

        public override ITimer CreateTimer(TimerCallback cb, TimeSpan timeOut, object state)
        {
            return new AsyncTimer(new
                System.Threading.Timer(s => cb(s), state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
        }

        public override Exception CreateTimeoutException(Exception ex, string errorMsg)
        {
            return new WebException("The request timed out", ex, WebExceptionStatus.Timeout, null);
        }
    }
}
#endif
