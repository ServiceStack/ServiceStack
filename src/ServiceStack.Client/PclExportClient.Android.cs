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
    }
}
#endif
