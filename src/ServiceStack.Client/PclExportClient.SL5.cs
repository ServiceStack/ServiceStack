//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if SL5
using System;
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Pcl;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Sl5PclExportClient : PclExportClient
    {
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
    }
}
#endif