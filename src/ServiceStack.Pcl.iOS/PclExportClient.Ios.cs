//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if __IOS__
using System;
using System.Collections.Specialized;
using System.Threading;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack
{
    public class IosPclExportClient : PclExportClient
    {
        public static IosPclExportClient Provider = new IosPclExportClient();

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
        }

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new IosPclExportClient()));
            IosPclExport.Configure();
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