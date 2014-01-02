//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETFX_CORE
using System;
using System.Threading;
using ServiceStack.Pcl;
using ServiceStack.Web;
using Windows.System.Threading;

namespace ServiceStack
{
    public class WinStorePclExportClient : PclExportClient
    {
        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return HttpUtility.ParseQueryString(query).InWrapper();
        }

        public override ITimer CreateTimer<TResponse>(AsyncState<TResponse> state, TimeSpan timeOut)
        {
            return new WinStoreAsyncTimer(ThreadPoolTimer.CreateTimer(state.TimedOut, timeOut));
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