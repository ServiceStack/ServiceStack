//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if ANDROID
using System;
using System.Collections.Specialized;
using System.Threading;
using Android.OS;
using ServiceStack.Web;

namespace ServiceStack
{
    public class AndroidPclExportClient : PclExportClient
    {
        public static AndroidPclExportClient Provider = new AndroidPclExportClient();

        public Handler UIHandler;

        public SynchronizationContext UiContext;

        public Action<Action> RunOnUiThreadFn;

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
        }

        public override void RunOnUiThread(Action fn)
        {
            if (RunOnUiThreadFn != null)
            {
                RunOnUiThreadFn(fn);
            }
            else
            {
                UiContext.Post(_ => fn(), null);
                //UIHandler.Post(fn);
            }
        }

        public static void Configure(Action<Action> runOnUiThread=null)
        {
            Provider.RunOnUiThreadFn = runOnUiThread;
            Provider.UIHandler = new Handler(Looper.MainLooper);
            Provider.UiContext = SynchronizationContext.Current;
            Instance = Provider;
            AndroidPclExport.Configure();
        }
    }
}
#endif
