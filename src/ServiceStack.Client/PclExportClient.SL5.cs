//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if SL5
using System;
using System.Collections.Specialized;
using System.Threading;
using System.Web;
using ServiceStack.Pcl;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Sl5PclExportClient : PclExportClient
    {
        public static Sl5PclExportClient Provider = new Sl5PclExportClient();

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return HttpUtility.ParseQueryString(query).InWrapper();
        }

        public override void RunOnUiThread(Action fn)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(fn);
        }

        public static void Configure()
        {
            Configure(Provider);
            Sl5PclExport.Configure();
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