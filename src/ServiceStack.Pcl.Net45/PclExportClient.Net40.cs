//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(XBOX || SL5 || NETFX_CORE || WP || PCL)
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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

        public override string GetHeader(WebHeaderCollection headers, string name, Func<string, bool> valuePredicate)
        {
            var values = headers.GetValues(name);
            return values == null ? null : values.FirstOrDefault(valuePredicate);
        }

        public override ITimer CreateTimer(TimerCallback cb, TimeSpan timeOut, object state)
        {
            return new AsyncTimer(new
                System.Threading.Timer(s => cb(s), state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
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
            if (Timer == null) return;
            
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
