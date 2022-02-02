//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if WP
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Pcl;
using ServiceStack.Web;

namespace ServiceStack
{
    public class WpPclExportClient : PclExportClient
    {
        public static WpPclExportClient Provider = new WpPclExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider);
            WpPclExport.Configure();
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
    }
}
#endif