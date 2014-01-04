//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if __IOS__
using System;
using System.Collections.Specialized;
using System.Threading;
using ServiceStack.Web;

namespace ServiceStack
{
    public class IosPclExportClient : PclExportClient
    {
        public static IosPclExportClient Provider = new IosPclExportClient();

        public SynchronizationContext UiContext;

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
            UiContext.Post(_ => fn(), null);
        }

        public static void Configure()
        {
            Provider.UiContext = SynchronizationContext.Current;
            Instance = Provider;
            IosPclExport.Configure();
        }
    }
}
#endif