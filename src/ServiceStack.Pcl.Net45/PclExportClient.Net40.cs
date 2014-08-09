//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(XBOX || SL5 || NETFX_CORE || WP || PCL)
using System;
using System.Collections.Specialized;
using System.Threading;
using System.Web;
using ServiceStack;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Net40PclExportClient : PclExportClient
    {
        public static Net40PclExportClient Provider = new Net40PclExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new Net40PclExportClient()));
            Net40PclExport.Configure();
            return Provider;
        }

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return HttpUtility.ParseQueryString(query).InWrapper();
        }

        public override string UrlEncode(string url)
        {
            return HttpUtility.UrlEncode(url);
        }

        public override string UrlDecode(string url)
        {
            return HttpUtility.UrlDecode(url);
        }

        public override string HtmlEncode(string html)
        {
            return HttpUtility.HtmlEncode(html);
        }

        public override string HtmlDecode(string html)
        {
            return HttpUtility.HtmlDecode(html);
        }

        public override ITimer CreateTimer<TResponse>(AsyncState<TResponse> state, TimeSpan timeOut)
        {
            return new AsyncTimer(new
                System.Threading.Timer(state.TimedOut, state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
        }
    }

    public class AsyncTimer : ITimer
    {
        public System.Threading.Timer Timer;

        public AsyncTimer(System.Threading.Timer timer)
        {
            Timer = timer;
        }

        public void Cancel()
        {
            this.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.Dispose();
        }

        public void Dispose()
        {
            if (Timer == null) return;

            this.Timer.Dispose();
            this.Timer = null;
        }
    }
}
#endif
