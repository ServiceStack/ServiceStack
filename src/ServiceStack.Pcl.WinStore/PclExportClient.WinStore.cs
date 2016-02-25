//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETFX_CORE
using System;
using System.IO;
using System.Threading;
using ServiceStack.Pcl;
using ServiceStack.Web;
using Windows.System.Threading;

namespace ServiceStack
{
    public class WinStorePclExportClient : PclExportClient
    {
        public static WinStorePclExportClient Provider = new WinStorePclExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new WinStorePclExportClient()));
            WinStorePclExport.Configure();
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

        public override ITimer CreateTimer(TimerCallback cb, TimeSpan timeOut, object state)
        {
            return new WinStoreAsyncTimer(ThreadPoolTimer.CreateTimer(s => cb(s), timeOut));
        }

        public override void RunOnUiThread(Action fn)
        {
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () => fn());
        }

        public override void CloseReadStream(Stream stream)
        {
            stream.Dispose();
        }

        public override void CloseWriteStream(Stream stream)
        {
            stream.Flush();
            stream.Dispose();
        }
    }

    public class WinStoreAsyncTimer : ITimer
    {
        public ThreadPoolTimer Timer;

        public WinStoreAsyncTimer(ThreadPoolTimer timer)
        {
            Timer = timer;
        }

        public void Cancel()
        {
            this.Timer.Cancel();
        }

        public void Dispose()
        {
            this.Timer = null;
        }
    }

}
#endif